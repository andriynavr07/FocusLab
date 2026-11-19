using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using FocusLab.Application.Abstractions;
using FocusLab.Application.Focus;
using Frontend.Services;

namespace Frontend.ViewModels;

public class TimelineEntry : BoardBase
{
    public DateTime StartedAt { get; init; }
    public int DurationMinutes { get; init; }
    public string Text => UiPreferences.Instance.Language == "English"
        ? $"{StartedAt:HH:mm} - Work ({DurationMinutes} min)"
        : $"{StartedAt:HH:mm} - Робота ({DurationMinutes}м)";
    public string State { get; init; } = "Pending"; // Done | Active | Pending
    public void RefreshLanguage() => OnPropertyChanged(nameof(Text));
}

public class PresetOption : BoardBase
{
    private bool _isSelected;
    private string _name = "";
    public string Name { get => UiPreferences.Instance.Translate(_name); init => _name = value; }
    public int Work { get; init; }
    public int Break { get; init; }
    public string Subtitle => $"{Uk.Duration(Work)} / {Uk.Duration(Break)}";
    public bool IsSelected { get => _isSelected; set => SetField(ref _isSelected, value); }
    public void RefreshLanguage()
    {
        OnPropertyChanged(nameof(Name));
        OnPropertyChanged(nameof(Subtitle));
    }
}

public sealed record FocusTaskOption(int? Id, string Title);

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
    private readonly TaskBoardStore _taskStore;

    private double _work, _short, _long;
    private int _sessionsPerRound;
    private bool _autoStart;
    private bool _saveInterruptedSessions;
    private FocusTaskOption? _selectedTaskOption;
    private bool _isFocusMode;
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

    public TimerViewModel(IFocusTimerService timer, IFocusSettingsProvider settings, TaskBoardStore taskStore)
    {
        _timer = timer;
        _settings = settings;
        _taskStore = taskStore;
        _dispatcher = Dispatcher.CurrentDispatcher; // VM створюється в UI-потоці

        var s = _settings.Current;
        _work = s.FocusMinutes;
        _short = s.ShortBreakMinutes;
        _long = s.LongBreakMinutes;
        _sessionsPerRound = s.SessionsBeforeLongBreak;
        _autoStart = s.AutoStartNext;
        _saveInterruptedSessions = s.SaveInterruptedSessions;

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
        ToggleFocusModeCommand = new RelayCommand(_ => IsFocusMode = !IsFocusMode);
        ExitFocusModeCommand = new RelayCommand(_ => IsFocusMode = false);
        _taskStore.Changed += OnTasksChanged;
        RefreshTasks();

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
            Interval = TimeSpan.FromSeconds(1)
        };
        _uiTimer.Tick += (_, _) => Refresh();
        _uiTimer.Start();

        Refresh();
    }

    /// <summary>Фокус-інтервал завершено або перервано (сесію збережено) — головна оновлює статистику.</summary>
    public event EventHandler? FocusSessionFinished;

    public ObservableCollection<PresetOption> Presets { get; }
    public ObservableCollection<TimelineEntry> Timeline { get; }
    public ObservableCollection<FocusTaskOption> TaskOptions { get; } = [];
    public string[] NoiseOptions => new[] { "Звуки природи", "Білий шум", "Тиша" }
        .Select(UiPreferences.Instance.Translate).ToArray();

    public ICommand ToggleCommand { get; }
    public ICommand ResetCommand { get; }
    public ICommand PauseCommand { get; }
    public ICommand SkipCommand { get; }
    public ICommand SelectPresetCommand { get; }
    public ICommand ToggleFocusModeCommand { get; }
    public ICommand ExitFocusModeCommand { get; }

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

    public bool SaveInterruptedSessions
    {
        get => _saveInterruptedSessions;
        set { if (SetField(ref _saveInterruptedSessions, value)) ScheduleSave(); }
    }

    public FocusTaskOption? SelectedTaskOption
    {
        get => _selectedTaskOption;
        set
        {
            if (!SetField(ref _selectedTaskOption, value)) return;
            if (_timer.Status == TimerStatus.Idle)
                _timer.SelectTask(value?.Id);
            else
                RefreshTasks();
            OnPropertyChanged(nameof(CurrentTaskTitle));
        }
    }

    public string CurrentTaskTitle =>
        TaskOptions.FirstOrDefault(option => option.Id == _timer.TaskId)?.Title
        ?? UiPreferences.Instance.Translate("Без задачі");
    public bool IsFocusMode
    {
        get => _isFocusMode;
        set => SetField(ref _isFocusMode, value);
    }

    public string SelectedNoise
    {
        get => UiPreferences.Instance.Translate(_selectedNoise);
        set
        {
            if (value is null) return;
            var source = new[] { "Звуки природи", "Білий шум", "Тиша" }
                .FirstOrDefault(option => UiPreferences.Instance.Translate(option) == value) ?? value;
            SetField(ref _selectedNoise, source);
        }
    }

    public string WorkText => Uk.Duration((int)_work);
    public string ShortText => Uk.Duration((int)_short);
    public string LongText => Uk.Duration((int)_long);

    // ----- відображення -----
    public bool IsRunning { get => _isRunning; private set => SetField(ref _isRunning, value); }
    public string TimeText { get => _timeText; private set => SetField(ref _timeText, value); }
    public double Progress { get => _progress; private set => SetField(ref _progress, value); }
    public string SessionText { get => _sessionText; private set => SetField(ref _sessionText, value); }
    public string StartPauseText { get => _startPauseText; private set => SetField(ref _startPauseText, value); }
    public string StartPauseGlyph { get => _startPauseGlyph; private set => SetField(ref _startPauseGlyph, value); }
    public string GoalText { get => _goalText; private set => SetField(ref _goalText, value); }
    public double GoalPercent { get => _goalPercent; private set => SetField(ref _goalPercent, value); }

    protected override void OnLanguageChanged()
    {
        OnPropertyChanged(nameof(NoiseOptions));
        OnPropertyChanged(nameof(SelectedNoise));
        OnPropertyChanged(nameof(WorkText));
        OnPropertyChanged(nameof(ShortText));
        OnPropertyChanged(nameof(LongText));
        OnPropertyChanged(nameof(CurrentTaskTitle));
        if (TaskOptions.Count > 0)
        {
            TaskOptions[0] = new FocusTaskOption(null, UiPreferences.Instance.Translate("Без задачі"));
            if (_selectedTaskOption?.Id is null)
            {
                _selectedTaskOption = TaskOptions[0];
                OnPropertyChanged(nameof(SelectedTaskOption));
            }
        }
        foreach (var preset in Presets)
            preset.RefreshLanguage();
        foreach (var entry in Timeline)
            entry.RefreshLanguage();
        Refresh();
    }

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
            MessageBox.Show(UiPreferences.Instance.Translate("Задача ще зберігається в базі даних. Спробуйте за мить."),
                "FocusLab", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (_timer.HasActiveSession && _timer.TaskId != row.Id)
        {
            MessageBox.Show(UiPreferences.Instance.Translate("Таймер уже працює. Зупиніть його, щоб почати фокус на іншій задачі."),
                "FocusLab", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        switch (_timer.Status)
        {
            case TimerStatus.Idle:
                SelectedTaskOption = TaskOptions.FirstOrDefault(option => option.Id == row.Id)
                    ?? new FocusTaskOption(row.Id, row.Title);
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
            case TimerStatus.Idle: _timer.Start(SelectedTaskOption?.Id); break;
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
            MessageBox.Show(UiPreferences.Instance.Translate("Помилка зупинки таймера") + ": " + ex.Message,
                "FocusLab", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        Refresh();
        FocusSessionFinished?.Invoke(this, EventArgs.Empty);
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

    private void OnTasksChanged(object? sender, EventArgs e)
        => _dispatcher.InvokeAsync(RefreshTasks);

    private void RefreshTasks()
    {
        if (_disposed) return;

        var selectedId = _timer.TaskId ?? _selectedTaskOption?.Id;
        var selectedTitle = _selectedTaskOption?.Title;
        TaskOptions.Clear();
        TaskOptions.Add(new FocusTaskOption(null, "Без задачі"));
        foreach (var row in _taskStore.Rows.Where(row => row.Id > 0))
            TaskOptions.Add(new FocusTaskOption(row.Id, row.Title));

        if (selectedId is int taskId && TaskOptions.All(option => option.Id != taskId)
            && selectedTitle is not null)
            TaskOptions.Add(new FocusTaskOption(taskId, selectedTitle));

        _selectedTaskOption = TaskOptions.FirstOrDefault(option => option.Id == selectedId) ?? TaskOptions[0];
        OnPropertyChanged(nameof(SelectedTaskOption));
        OnPropertyChanged(nameof(CurrentTaskTitle));
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
                AutoStartNext = _autoStart,
                SaveInterruptedSessions = _saveInterruptedSessions
            });
        }
        catch (Exception ex)
        {
            // У пам'яті налаштування вже діють; не вдалося лише записати файл.
            MessageBox.Show(UiPreferences.Instance.Translate("Налаштування таймера не збережено у файл") + ": " + ex.Message,
                "FocusLab", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        _timer.ApplySettings();
        OnPropertyChanged(nameof(CurrentTaskTitle));
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
                StartedAt = DateTime.Now,
                DurationMinutes = (int)Math.Round(_lastFocusDuration.TotalMinutes),
                State = "Done"
            });
            FocusSessionFinished?.Invoke(this, EventArgs.Empty);
        }
        Refresh();
    });

    private void OnError(object? sender, TimerErrorEventArgs e) => _dispatcher.InvokeAsync(() =>
    {
        if (_saveErrorShown) return;
        _saveErrorShown = true;
        MessageBox.Show(
            UiPreferences.Instance.Translate(
                "Не вдалося зберегти фокус-сесію в базу даних. Дані не втрачено в пам'яті — застосунок повторить спробу після наступної успішної операції."),
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

        SessionText = UiPreferences.Instance.Language == "English"
            ? phase switch
            {
                TimerPhase.Focus => $"SESSION {_timer.CurrentSessionNumber} OF {_timer.SessionsPerCycle}",
                TimerPhase.ShortBreak => "SHORT BREAK",
                _ => "LONG BREAK"
            }
            : phase switch
            {
                TimerPhase.Focus => $"СЕСІЯ {_timer.CurrentSessionNumber} З {_timer.SessionsPerCycle}",
                TimerPhase.ShortBreak => "КОРОТКА ПЕРЕРВА",
                _ => "ДОВГА ПЕРЕРВА"
            };
        OnPropertyChanged(nameof(CurrentTaskTitle));

        IsRunning = status == TimerStatus.Running;
        StartPauseText = UiPreferences.Instance.Translate(IsRunning ? "ПАУЗА" : "ПОЧАТИ");
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
        GoalText = UiPreferences.Instance.Language == "English"
            ? $"{(total / 3600).ToString("0.0", CultureInfo.InvariantCulture)} of {GoalHours:0} hours"
            : $"{(total / 3600).ToString("0.0", CultureInfo.InvariantCulture)} з {GoalHours:0} годин";
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
        _taskStore.Changed -= OnTasksChanged;
    }
}
