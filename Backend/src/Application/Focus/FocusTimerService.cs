using System.Collections.Concurrent;
using FocusLab.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace FocusLab.Application.Focus;

/// <summary>
/// Логіка таймера в пам'яті (issue #80).
/// Залишок = тривалість − (UtcNow − startTime − pausedTotal): згортання вікна, затримки потоку
/// чи сон ПК не ламають відлік. Завершення інтервалу виявляє внутрішній «сторож», тому воно
/// спрацьовує навіть тоді, коли UI не відображається.
/// </summary>
public sealed class FocusTimerService : IFocusTimerService, IDisposable
{
    private static readonly TimeSpan DefaultWatchdogInterval = TimeSpan.FromMilliseconds(250);

    private readonly object _gate = new();
    private readonly IClock _clock;
    private readonly IFocusService _focusService;
    private readonly INotificationService _notifications;
    private readonly ISoundPlayer _sound;
    private readonly IFocusSettingsProvider _settings;
    private readonly ILogger<FocusTimerService> _logger;
    private readonly ConcurrentQueue<PendingSession> _pending = new();
    private readonly Timer _watchdog;

    private TimerPhase _phase = TimerPhase.Focus;
    private TimerStatus _status = TimerStatus.Idle;
    private int? _taskId;
    private DateTime _startUtc;
    private DateTime? _pausedAtUtc;
    private TimeSpan _pausedTotal;
    private TimeSpan _duration;
    private int _completedInCycle;

    /// <param name="watchdogInterval">Інтервал перевірки завершення. У тестах — Timeout.InfiniteTimeSpan
    /// (сторож вимкнено, тік викликається вручну через TickAsync).</param>
    public FocusTimerService(
        IClock clock,
        IFocusService focusService,
        INotificationService notifications,
        ISoundPlayer sound,
        IFocusSettingsProvider settings,
        ILogger<FocusTimerService> logger,
        TimeSpan? watchdogInterval = null)
    {
        _clock = clock;
        _focusService = focusService;
        _notifications = notifications;
        _sound = sound;
        _settings = settings;
        _logger = logger;

        _duration = DurationFor(TimerPhase.Focus);

        var interval = watchdogInterval ?? DefaultWatchdogInterval;
        _watchdog = new Timer(state => _ = TickAsync(), null, interval, interval);
    }

    public event EventHandler? StateChanged;
    public event EventHandler<IntervalCompletedEventArgs>? IntervalCompleted;
    public event EventHandler<TimerErrorEventArgs>? ErrorOccurred;

    // ───────────── Стан ─────────────

    public TimerPhase Phase { get { lock (_gate) return _phase; } }
    public TimerStatus Status { get { lock (_gate) return _status; } }
    public int? TaskId { get { lock (_gate) return _taskId; } }
    public TimeSpan Remaining { get { lock (_gate) return RemainingUnsafe(); } }
    public TimeSpan PhaseDuration { get { lock (_gate) return _duration; } }

    public double Progress
    {
        get
        {
            lock (_gate)
                return _duration <= TimeSpan.Zero ? 0 : ElapsedUnsafe().TotalSeconds / _duration.TotalSeconds;
        }
    }

    public int CompletedSessionsInCycle { get { lock (_gate) return _completedInCycle; } }
    public int SessionsPerCycle => Math.Max(1, _settings.Current.SessionsBeforeLongBreak);

    public int CurrentSessionNumber
    {
        get
        {
            var perCycle = SessionsPerCycle;
            lock (_gate)
                return _phase == TimerPhase.Focus
                    ? Math.Min(_completedInCycle + 1, perCycle)
                    : Math.Min(_completedInCycle, perCycle);
        }
    }

    public bool HasActiveSession { get { lock (_gate) return _status != TimerStatus.Idle; } }

    public bool CanStart
    {
        get { lock (_gate) return _status == TimerStatus.Idle; }
    }

    public int PendingUnsavedSessions => _pending.Count;

    // ───────────── Керування ─────────────

    public void SelectTask(int? taskId)
    {
        lock (_gate)
        {
            if (_status != TimerStatus.Idle) return;
            _taskId = taskId;
        }
        RaiseStateChanged();
    }

    public void Start(int? taskId = null)
    {
        lock (_gate)
        {
            if (_status != TimerStatus.Idle) return;
            if (taskId.HasValue) _taskId = taskId;
            // Без задачі таймер працює, але сесія не записується (FocusSessions.TaskId обов'язковий).
            BeginIntervalUnsafe();
        }
        RaiseStateChanged();
    }

    public void Pause()
    {
        lock (_gate)
        {
            if (_status != TimerStatus.Running) return;
            _pausedAtUtc = _clock.UtcNow;
            _status = TimerStatus.Paused;
        }
        RaiseStateChanged();
    }

    public void Resume()
    {
        lock (_gate)
        {
            if (_status != TimerStatus.Paused) return;
            _pausedTotal += _clock.UtcNow - _pausedAtUtc!.Value;
            _pausedAtUtc = null;
            _status = TimerStatus.Running;
        }
        RaiseStateChanged();
    }

    public async Task StopAsync()
    {
        PendingSession? partial = null;
        var saveInterrupted = _settings.Current.SaveInterruptedSessions;

        lock (_gate)
        {
            if (_status == TimerStatus.Idle) return;

            if (_phase == TimerPhase.Focus && _taskId is int id && saveInterrupted)
            {
                var minutes = (int)Math.Floor(ElapsedUnsafe().TotalMinutes);
                if (minutes >= 1)
                    partial = new PendingSession(id, _startUtc, ToMinutes(_duration), minutes, false);
            }
            BackToFocusIdleUnsafe();
        }
        RaiseStateChanged();

        if (partial is not null) await SaveAsync(partial);
    }

    public void SkipBreak()
    {
        lock (_gate)
        {
            if (_phase == TimerPhase.Focus) return;
            BackToFocusIdleUnsafe();
        }
        RaiseStateChanged();
    }

    public void ApplySettings()
    {
        lock (_gate)
        {
            if (_status != TimerStatus.Idle) return;
            _duration = DurationFor(_phase);
        }
        RaiseStateChanged();
    }

    // ───────────── Завершення інтервалу ─────────────

    /// <summary>Перевіряє, чи закінчився інтервал. Викликається сторожем; публічний для тестів.</summary>
    public async Task TickAsync()
    {
        IntervalCompletedEventArgs args;
        PendingSession? session = null;

        lock (_gate)
        {
            if (_status != TimerStatus.Running || RemainingUnsafe() > TimeSpan.Zero) return;

            var finished = _phase;
            if (finished == TimerPhase.Focus && _taskId is int id)
            {
                var minutes = ToMinutes(_duration);
                session = new PendingSession(id, _startUtc, minutes, minutes, true);
            }

            // Стан міняємо одразу (до запису в БД): UI миттєво переходить на перерву,
            // а повторний тік не зарахує той самий інтервал удруге.
            var next = AdvanceUnsafe(finished);
            args = new IntervalCompletedEventArgs(finished, next, _taskId);
        }

        try
        {
            RaiseStateChanged();

            try
            {
                _sound.PlayIntervalCompleted();
                _notifications.NotifyIntervalCompleted(args.Finished, args.Next);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Не вдалося показати сповіщення про завершення інтервалу");
            }

            if (session is not null) await SaveAsync(session);

            // Подію піднімаємо після запису, щоб аналітика бачила нову сесію.
            Raise(() => IntervalCompleted?.Invoke(this, args));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Помилка обробки завершення інтервалу");
        }
    }

    private TimerPhase AdvanceUnsafe(TimerPhase finished)
    {
        TimerPhase next;
        if (finished == TimerPhase.Focus)
        {
            _completedInCycle++;
            next = _completedInCycle >= SessionsPerCycle ? TimerPhase.LongBreak : TimerPhase.ShortBreak;
        }
        else
        {
            if (finished == TimerPhase.LongBreak) _completedInCycle = 0;
            next = TimerPhase.Focus;
        }

        SetPhaseIdleUnsafe(next);
        if (_settings.Current.AutoStartNext)
            BeginIntervalUnsafe();
        return next;
    }

    // ───────────── Запис сесій ─────────────

    private async Task SaveAsync(PendingSession s)
    {
        try
        {
            await WriteAsync(s);
        }
        catch (KeyNotFoundException ex)
        {
            // Задачу видалено під час сесії — зберігати нікуди, повторні спроби безглузді.
            _logger.LogWarning(ex, "Сесію не збережено: задачу {TaskId} видалено", s.TaskId);
            return;
        }
        catch (Exception ex)
        {
            _pending.Enqueue(s);
            _logger.LogError(ex, "Не вдалося записати фокус-сесію (TaskId={TaskId})", s.TaskId);
            Raise(() => ErrorOccurred?.Invoke(this, new TimerErrorEventArgs("SaveSession", ex)));
            return;
        }

        // БД доступна — доганяємо сесії, що не збереглися раніше.
        await RetryPendingAsync();
    }

    public async Task RetryPendingAsync()
    {
        var count = _pending.Count;
        for (var i = 0; i < count && _pending.TryDequeue(out var s); i++)
        {
            try
            {
                await WriteAsync(s);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Відкладену сесію відкинуто: задачу {TaskId} видалено", s.TaskId);
            }
            catch (Exception ex)
            {
                _pending.Enqueue(s);
                _logger.LogError(ex, "Повторний запис сесії не вдався");
                Raise(() => ErrorOccurred?.Invoke(this, new TimerErrorEventArgs("RetrySaveSession", ex)));
                return;
            }
        }
    }

    private Task WriteAsync(PendingSession s) =>
        _focusService.SaveSessionAsync(s.TaskId, s.StartUtc, s.EstimatedMinutes, s.DurationMinutes, s.IsCompleted);

    // ───────────── Допоміжне (Unsafe-методи викликаються лише під lock) ─────────────

    private TimeSpan ElapsedUnsafe()
    {
        if (_status == TimerStatus.Idle) return TimeSpan.Zero;
        var reference = _status == TimerStatus.Paused ? _pausedAtUtc!.Value : _clock.UtcNow;
        var elapsed = reference - _startUtc - _pausedTotal;
        if (elapsed < TimeSpan.Zero) return TimeSpan.Zero;
        return elapsed > _duration ? _duration : elapsed;
    }

    private TimeSpan RemainingUnsafe() => _status == TimerStatus.Idle ? _duration : _duration - ElapsedUnsafe();

    private void BeginIntervalUnsafe()
    {
        _startUtc = _clock.UtcNow;
        _pausedAtUtc = null;
        _pausedTotal = TimeSpan.Zero;
        _status = TimerStatus.Running;
    }

    private void SetPhaseIdleUnsafe(TimerPhase phase)
    {
        _phase = phase;
        _duration = DurationFor(phase);
        _status = TimerStatus.Idle;
        _pausedAtUtc = null;
        _pausedTotal = TimeSpan.Zero;
    }

    private void BackToFocusIdleUnsafe()
    {
        if (_phase == TimerPhase.LongBreak) _completedInCycle = 0;
        SetPhaseIdleUnsafe(TimerPhase.Focus);
    }

    private TimeSpan DurationFor(TimerPhase phase)
    {
        var s = _settings.Current;
        var minutes = phase switch
        {
            TimerPhase.Focus => s.FocusMinutes,
            TimerPhase.ShortBreak => s.ShortBreakMinutes,
            _ => s.LongBreakMinutes
        };
        return TimeSpan.FromMinutes(Math.Max(1, minutes));
    }

    private static int ToMinutes(TimeSpan duration) => (int)Math.Round(duration.TotalMinutes);

    private void RaiseStateChanged() => Raise(() => StateChanged?.Invoke(this, EventArgs.Empty));

    /// <summary>Виняток у підписника не повинен валити потік таймера.</summary>
    private void Raise(Action raise)
    {
        try { raise(); }
        catch (Exception ex) { _logger.LogError(ex, "Помилка в обробнику події таймера"); }
    }

    public void Dispose() => _watchdog.Dispose();
}
