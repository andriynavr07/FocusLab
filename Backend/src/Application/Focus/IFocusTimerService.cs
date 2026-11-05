namespace FocusLab.Application.Focus;

/// <summary>
/// Таймер Pomodoro (singleton). Увесь стан — у пам'яті; у БД пише лише через IFocusService.
/// УВАГА: події піднімаються з потоку пулу — ViewModel має маршалити їх у Dispatcher.
/// </summary>
public interface IFocusTimerService
{
    TimerPhase Phase { get; }
    TimerStatus Status { get; }
    int? TaskId { get; }

    /// <summary>Залишок часу. Обчислюється від startTime, а не лічильником тіків.</summary>
    TimeSpan Remaining { get; }
    TimeSpan PhaseDuration { get; }

    /// <summary>Частка минулого часу 0..1.</summary>
    double Progress { get; }

    int CompletedSessionsInCycle { get; }
    int SessionsPerCycle { get; }

    /// <summary>Для підпису «СЕСІЯ 3 З 4».</summary>
    int CurrentSessionNumber { get; }

    /// <summary>Є незавершений інтервал — показати попередження при закритті застосунку (SC-06, A2).</summary>
    bool HasActiveSession { get; }
    bool CanStart { get; }
    int PendingUnsavedSessions { get; }

    event EventHandler? StateChanged;
    event EventHandler<IntervalCompletedEventArgs>? IntervalCompleted;
    event EventHandler<TimerErrorEventArgs>? ErrorOccurred;

    /// <summary>Обирає задачу, поки таймер у стані Idle.</summary>
    void SelectTask(int? taskId);

    /// <summary>Запускає поточну фазу. Задача береться з аргументу або з SelectTask; без задачі
    /// таймер працює, але сесія в БД не записується.</summary>
    void Start(int? taskId = null);
    void Pause();
    void Resume();

    /// <summary>Достроково перериває інтервал і повертає таймер у Idle (фаза Focus).</summary>
    Task StopAsync();

    /// <summary>Пропускає перерву (SC-07, A1): наступна фокус-сесія стартує вручну.</summary>
    void SkipBreak();

    /// <summary>Застосовує нові тривалості, якщо таймер у Idle.</summary>
    void ApplySettings();

    /// <summary>Повторна спроба запису сесій, що не збереглися.</summary>
    Task RetryPendingAsync();
}
