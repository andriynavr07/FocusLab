using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using FocusLab.Application.Abstractions;
using FocusLab.Application.Focus;
using Frontend.Services;

namespace Frontend.ViewModels;

public class TimelineEntry
{
    public string Text { get; init; } = "";
    public string State { get; init; } = "Pending"; // Done | Active | Pending
}

public class PresetOption : BoardBase
{
    private bool _isSelected;
    public string Name { get; init; } = "";
    public int Work { get; init; }
    public int Break { get; init; }
    public string Subtitle => $"{Work} хв / {Break} хв";
    public bool IsSelected { get => _isSelected; set => SetField(ref _isSelected, value); }
}

/// <summary>
/// Адаптер між UI і IFocusTimerService. Уся логіка відліку, перерв, запису сесій і сповіщень —
/// у сервісі (singleton); тут лише відображення стану та команди. Публічний інтерфейс
/// (властивості й команди для TimerView / DashboardView / SettingsViewModel) збережено.
/// Події сервісу приходять з потоку пулу, тому маршалимо їх у Dispatcher.
/// </summary>
public sealed class TimerViewModel : PageViewModelBase, IDisposable
{
    private const double GoalHours = 6;

    private readonly IFocusTimerService _timer;
    private readonly IFocusSettingsProvider _settings;
    private readonly Dispatcher _dispatcher;
    private readonly DispatcherTimer _uiTimer;
    private readonly DispatcherTimer _saveDebounce;

    private double _work, _short, _long;
    private int _sessionsPerRound;
    private bool _autoStart;
    private string _selectedNoise = "Звуки природи";

    private string _timeText = "";
    private double _progress = 1;
    private string _sessionText = "";
    private string _startPauseText = "ПОЧАТИ";
    private string _startPauseGlyph = "\uE768";
    private bool _isRunning;
    private string _goalText = "";
    private double _goalPercent;

    // Облік «фокус-часу сьогодні» (лише в межах роботи застосунку).
    private double _focusedBaseSeconds;
    private double _lastFocusElapsed;
    private TimeSpan _lastFocusDuration = TimeSpan.FromMinutes(25);
    private bool _wasInFocus;

    private bool _saveErrorShown;
    private bool _disposed;

    public TimerViewModel(IFocusTimerService timer, IFocusSettingsProvider settings)
    {
        _timer = timer;
        _settings = settings;
        _dispatcher = Dispatcher.CurrentDispatcher; // VM створюється в UI-потоці

        var s = _settings.Current;
        _work = s.FocusMinutes;
        _short = s.ShortBreakMinutes;
        _long = s.LongBreakMinutes;
        _sessionsPerRound = s.SessionsBeforeLongBreak;
        _autoStart = s.AutoStartNext;

        Presets = new ObservableCollection<PresetOption>
        {
            new() { Name = "Рекомендовано", Work = 25, Break = 5 },
            new() { Name = "Довга сесія", Work = 50, Break = 10 },
            new() { Name = "Швидка сесія", Work = 15, Break = 3 }
        };
        SyncPresetSelection();

        Timeline = new ObservableCollection<TimelineEntry>();

        ToggleCommand = new RelayCommand(_ => Toggle());
        // «Скинути»: перервати поточний інтервал і повернути повний час.
        ResetCommand = new RelayCommand(async _ => await StopAsync());
        PauseCommand = new RelayCommand(_ => { _timer.Pause(); Refresh(); });
        // «Пропустити»: під час перерви — одразу до наступної сесії; під час роботи — перервати інтервал.
        SkipCommand = new RelayCommand(async _ => await SkipAsync());
        SelectPresetCommand = new RelayCommand(p => { if (p is PresetOption o) ApplyPreset(o); });

        _saveDebounce = new DispatcherTimer(DispatcherPriority.Background, _dispatcher)
        {
            Interval = TimeSpan.FromMilliseconds(400)
        };
        _saveDebounce.Tick += (_, _) => SaveSettings();

        _timer.StateChanged += OnStateChanged;
        _timer.IntervalCompleted += OnIntervalCompleted;
        _timer.ErrorOccurred += OnError;

        _uiTimer = new DispatcherTimer(DispatcherPriority.Normal, _dispatcher)
        {
            Interval = TimeSpan.FromMilliseconds(250)
        };
        _uiTimer.Tick += (_, _) => Refresh();
        _uiTimer.Start();

        Refresh();
    }

    public ObservableCollection<PresetOption> Presets { get; }
    public ObservableCollection<TimelineEntry> Timeline { get; }
    public string[] NoiseOptions { get; } = { "Звуки природи", "Білий шум", "Тиша" };

    public ICommand ToggleCommand { get; }
    public ICommand ResetCommand { get; }
    public ICommand PauseCommand { get; }
    public ICommand SkipCommand { get; }
    public ICommand SelectPresetCommand { get; }

    // ----- налаштування (зберігаються в settings.json із затримкою 400 мс) -----
    public double WorkMinutes
    {
        get => _work;
        set { if (SetField(ref _work, value)) { OnPropertyChanged(nameof(WorkText)); SyncPresetSelection(); ScheduleSave(); } }
    }

    public double ShortBreakMinutes
    {
        get => _short;
        set { if (SetField(ref _short, value)) { OnPropertyChanged(nameof(ShortText)); SyncPresetSelection(); ScheduleSave(); } }
    }

    public double LongBreakMinutes
    {
        get => _long;
        set { if (SetField(ref _long, value)) { OnPropertyChanged(nameof(LongText)); ScheduleSave(); } }
    }

    public bool AutoStart
    {
        get => _autoStart;
        set { if (SetField(ref _autoStart, value)) ScheduleSave(); }
    }

    public int SessionsPerRound
    {
        get => _sessionsPerRound;
        set
        {
            if (value is < 2 or > 10 || !SetField(ref _sessionsPerRound, value)) return;
            ScheduleSave();
        }
    }

    public string SelectedNoise { get => _selectedNoise; set { if (value != null) SetField(ref _selectedNoise, value); } }

    public string WorkText => $"{(int)_work} хв";
    public string ShortText => $"{(int)_short} хв";
    public string LongText => $"{(int)_long} хв";

    // ----- відображення -----
    public bool IsRunning { get => _isRunning; private set => SetField(ref _isRunning, value); }
    public string TimeText { get => _timeText; private set => SetField(ref _timeText, value); }
    public double Progress { get => _progress; private set => SetField(ref _progress, value); }
    public string SessionText { get => _sessionText; private set => SetField(ref _sessionText, value); }
    public string StartPauseText { get => _startPauseText; private set => SetField(ref _startPauseText, value); }
    public string StartPauseGlyph { get => _startPauseGlyph; private set => SetField(ref _startPauseGlyph, value); }
    public string GoalText { get => _goalText; private set => SetField(ref _goalText, value); }
    public double GoalPercent { get => _goalPercent; private set => SetField(ref _goalPercent, value); }

    // ═════════ Керування ═════════

    /// <summary>Запускає фокус-сесію для задачі (кнопка «Старт фокусу» на картці задачі).</summary>
    public void StartFocusFor(TaskRow? row)
    {
        if (row is null)
        {
            Toggle();
            return;
        }

        if (row.Id <= 0)
        {
            MessageBox.Show("Задача ще зберігається в базі даних. Спробуйте за мить.",
                "FocusLab", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (_timer.HasActiveSession && _timer.TaskId != row.Id)
        {
            MessageBox.Show("Таймер уже працює. Зупиніть його, щоб почати фокус на іншій задачі.",
                "FocusLab", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        switch (_timer.Status)
        {
            case TimerStatus.Idle:
                if (_timer.Phase != TimerPhase.Focus) _timer.SkipBreak(); // з паузи між фазами — одразу до роботи
                _timer.Start(row.Id);
                break;
            case TimerStatus.Paused:
                _timer.Resume();
                break;
        }
        Refresh();
    }

    private void Toggle()
    {
        switch (_timer.Status)
        {
            case TimerStatus.Idle: _timer.Start(); break;
            case TimerStatus.Running: _timer.Pause(); break;
            case TimerStatus.Paused: _timer.Resume(); break;
        }
        Refresh();
    }

    private async Task StopAsync()
    {
        try
        {
            await _timer.StopAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show("Помилка зупинки таймера: " + ex.Message,
                "FocusLab", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        Refresh();
    }

    private async Task SkipAsync()
    {
        if (_timer.Phase == TimerPhase.Focus)
            await StopAsync();
        else
        {
            _timer.SkipBreak();
            Refresh();
        }
    }

    private void ApplyPreset(PresetOption o)
    {
        WorkMinutes = o.Work;
        ShortBreakMinutes = o.Break;
    }

    private void SyncPresetSelection()
    {
        foreach (var p in Presets)
            p.IsSelected = p.Work == (int)_work && p.Break == (int)_short;
    }

    // ═════════ Налаштування ═════════

    private void ScheduleSave()
    {
        _saveDebounce.Stop();
        _saveDebounce.Start();
    }

    private void SaveSettings()
    {
        _saveDebounce.Stop();
        try
        {
            _settings.Save(_settings.Current with
            {
                FocusMinutes = (int)Math.Round(_work),
                ShortBreakMinutes = (int)Math.Round(_short),
                LongBreakMinutes = (int)Math.Round(_long),
                SessionsBeforeLongBreak = _sessionsPerRound,
                AutoStartNext = _autoStart
            });
        }
        catch (Exception ex)
        {
            // У пам'яті налаштування вже діють; не вдалося лише записати файл.
            MessageBox.Show("Налаштування таймера не збережено у файл: " + ex.Message,
                "FocusLab", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        _timer.ApplySettings();
        Refresh();
    }

    // ═════════ Події сервісу ═════════

    private void OnStateChanged(object? sender, EventArgs e) => _dispatcher.InvokeAsync(Refresh);

    private void OnIntervalCompleted(object? sender, IntervalCompletedEventArgs e) => _dispatcher.InvokeAsync(() =>
    {
        if (e.Finished == TimerPhase.Focus)
        {
            Timeline.Add(new TimelineEntry
            {
                Text = $"{DateTime.Now:HH:mm} - Робота ({(int)Math.Round(_lastFocusDuration.TotalMinutes)}м)",
                State = "Done"
            });
        }
        Refresh();
    });

    private void OnError(object? sender, TimerErrorEventArgs e) => _dispatcher.InvokeAsync(() =>
    {
        if (_saveErrorShown) return;
        _saveErrorShown = true;
        MessageBox.Show(
            "Не вдалося зберегти фокус-сесію в базу даних. Дані не втрачено в пам'яті — " +
            "застосунок повторить спробу після наступної успішної операції.",
            "FocusLab", MessageBoxButton.OK, MessageBoxImage.Warning);
    });

    // ═════════ Оновлення відображення ═════════

    private void Refresh()
    {
        if (_disposed) return;

        var status = _timer.Status;
        var phase = _timer.Phase;
        var remaining = _timer.Remaining;
        var duration = _timer.PhaseDuration;

        var seconds = (int)Math.Ceiling(remaining.TotalSeconds);
        TimeText = $"{seconds / 60:00}:{seconds % 60:00}";
        Progress = duration <= TimeSpan.Zero ? 0 : Math.Clamp(remaining.TotalSeconds / duration.TotalSeconds, 0, 1);

        SessionText = phase switch
        {
            TimerPhase.Focus => $"СЕСІЯ {_timer.CurrentSessionNumber} З {_timer.SessionsPerCycle}",
            TimerPhase.ShortBreak => "КОРОТКА ПЕРЕРВА",
            _ => "ДОВГА ПЕРЕРВА"
        };

        IsRunning = status == TimerStatus.Running;
        StartPauseText = IsRunning ? "ПАУЗА" : "ПОЧАТИ";
        StartPauseGlyph = IsRunning ? "\uE769" : "\uE768";

        TrackFocusedTime(status, phase, remaining, duration);

        if (_timer.PendingUnsavedSessions == 0) _saveErrorShown = false;
    }

    private void TrackFocusedTime(TimerStatus status, TimerPhase phase, TimeSpan remaining, TimeSpan duration)
    {
        var inFocus = phase == TimerPhase.Focus && status != TimerStatus.Idle;

        if (inFocus)
        {
            _lastFocusDuration = duration;
            _lastFocusElapsed = (duration - remaining).TotalSeconds;
        }
        else if (_wasInFocus)
        {
            // Інтервал роботи щойно завершився або був перерваний — зараховуємо пропрацьований час.
            _focusedBaseSeconds += _lastFocusElapsed;
            _lastFocusElapsed = 0;
        }
        _wasInFocus = inFocus;

        var total = _focusedBaseSeconds + (inFocus ? _lastFocusElapsed : 0);
        GoalText = $"{(total / 3600).ToString("0.0", CultureInfo.InvariantCulture)} з {GoalHours:0} годин";
        GoalPercent = Math.Min(100, total / 3600 / GoalHours * 100);
    }

    public void Dispose()
    {
        if (_disposed) return;

        if (_saveDebounce.IsEnabled) SaveSettings(); // не втрачаємо останню зміну
        _disposed = true;

        _uiTimer.Stop();
        _saveDebounce.Stop();
        _timer.StateChanged -= OnStateChanged;
        _timer.IntervalCompleted -= OnIntervalCompleted;
        _timer.ErrorOccurred -= OnError;
    }
}
