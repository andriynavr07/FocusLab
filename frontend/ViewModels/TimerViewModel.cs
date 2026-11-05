using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows.Input;
using System.Windows.Threading;
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

public class TimerViewModel : PageViewModelBase
{
    private enum Mode { Work, ShortBreak, LongBreak }

    private int _sessionsTotal = 4;
    private const double GoalHours = 6;

    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromSeconds(1) };
    private double _work = 25, _short = 5, _long = 15;
    private bool _autoStart = true;
    private bool _running;
    private Mode _mode = Mode.Work;
    private int _session = 1;
    private int _remaining = 25 * 60;
    private int _phaseSeconds = 25 * 60;
    private double _focusedSeconds;
    private long _endTimestamp;
    private long _lastTimestamp;
    private string _selectedNoise = "Звуки природи";

    public TimerViewModel()
    {
        Presets = new ObservableCollection<PresetOption>
        {
            new() { Name = "Рекомендовано", Work = 25, Break = 5, IsSelected = true },
            new() { Name = "Довга сесія", Work = 50, Break = 10 },
            new() { Name = "Швидка сесія", Work = 15, Break = 3 }
        };

        Timeline = new ObservableCollection<TimelineEntry>();

        ToggleCommand = new RelayCommand(_ => Toggle());
        ResetCommand = new RelayCommand(_ => { Stop(); _phaseSeconds = ModeSeconds; _remaining = _phaseSeconds; RaiseTimer(); });
        PauseCommand = new RelayCommand(_ => Stop());
        SkipCommand = new RelayCommand(_ => { Stop(); Advance(completed: false); });
        SelectPresetCommand = new RelayCommand(p => { if (p is PresetOption o) ApplyPreset(o); });
        _timer.Tick += (_, _) => Tick();
    }

    public ObservableCollection<PresetOption> Presets { get; }
    public ObservableCollection<TimelineEntry> Timeline { get; }
    public string[] NoiseOptions { get; } = { "Звуки природи", "Білий шум", "Тиша" };

    public ICommand ToggleCommand { get; }
    public ICommand ResetCommand { get; }
    public ICommand PauseCommand { get; }
    public ICommand SkipCommand { get; }
    public ICommand SelectPresetCommand { get; }

    // ----- налаштування -----
    public double WorkMinutes { get => _work; set { if (SetField(ref _work, value)) { OnPropertyChanged(nameof(WorkText)); RefreshIdle(Mode.Work); } } }
    public double ShortBreakMinutes { get => _short; set { if (SetField(ref _short, value)) { OnPropertyChanged(nameof(ShortText)); RefreshIdle(Mode.ShortBreak); } } }
    public double LongBreakMinutes { get => _long; set { if (SetField(ref _long, value)) { OnPropertyChanged(nameof(LongText)); RefreshIdle(Mode.LongBreak); } } }
    public bool AutoStart { get => _autoStart; set => SetField(ref _autoStart, value); }
    public int SessionsPerRound
    {
        get => _sessionsTotal;
        set
        {
            if (value is < 1 or > 12 || !SetField(ref _sessionsTotal, value)) return;
            if (_session > _sessionsTotal) _session = 1;
            OnPropertyChanged(nameof(SessionText));
        }
    }
    public bool IsRunning => _running;
    public string SelectedNoise { get => _selectedNoise; set { if (value != null) SetField(ref _selectedNoise, value); } }

    public string WorkText => $"{(int)_work} хв";
    public string ShortText => $"{(int)_short} хв";
    public string LongText => $"{(int)_long} хв";

    // ----- відображення -----
    private int ModeSeconds => (int)(_mode switch { Mode.Work => _work, Mode.ShortBreak => _short, _ => _long } * 60);
    public string TimeText => $"{_remaining / 60:00}:{_remaining % 60:00}";
    public double Progress => _phaseSeconds == 0 ? 0 : (double)_remaining / _phaseSeconds;
    public string SessionText => $"СЕСІЯ {_session} З {_sessionsTotal}";
    public string StartPauseText => _running ? "ПАУЗА" : "ПОЧАТИ";
    public string StartPauseGlyph => _running ? "\uE769" : "\uE768";
    public string GoalText => $"{(_focusedSeconds / 3600).ToString("0.0", System.Globalization.CultureInfo.InvariantCulture)} з {GoalHours:0} годин";
    public double GoalPercent => Math.Min(100, _focusedSeconds / 3600 / GoalHours * 100);

    // ═════════ Логіка ═════════
    private void ApplyPreset(PresetOption o)
    {
        foreach (var p in Presets) p.IsSelected = ReferenceEquals(p, o);
        WorkMinutes = o.Work;
        ShortBreakMinutes = o.Break;
    }

    private void RefreshIdle(Mode changed)
    {
        if (_running || _mode != changed) return;
        _phaseSeconds = ModeSeconds;
        _remaining = _phaseSeconds;
        RaiseTimer();
    }

    private void Toggle()
    {
        if (_running) Stop();
        else
        {
            _running = true;
            var now = Stopwatch.GetTimestamp();
            _endTimestamp = now + (long)(_remaining * Stopwatch.Frequency);
            _lastTimestamp = now;
            _timer.Start();
            OnPropertyChanged(nameof(IsRunning));
            RaiseTimer();
        }
    }

    private void Stop()
    {
        if (_running)
        {
            var now = Stopwatch.GetTimestamp();
            if (_mode == Mode.Work)
                AddFocusedSeconds(now);
            UpdateRemaining(now);
        }
        _timer.Stop();
        _running = false;
        OnPropertyChanged(nameof(IsRunning));
        RaiseTimer();
    }

    private void Tick()
    {
        var now = Stopwatch.GetTimestamp();
        if (_mode == Mode.Work)
        {
            AddFocusedSeconds(now);
            OnPropertyChanged(nameof(GoalText));
            OnPropertyChanged(nameof(GoalPercent));
        }

        UpdateRemaining(now);
        if (_remaining <= 0)
        {
            Stop();
            Advance(completed: true);
            if (AutoStart) Toggle();
            return;
        }

        RaiseTimer();
    }

    private void AddFocusedSeconds(long now)
    {
        _focusedSeconds += (double)(now - _lastTimestamp) / Stopwatch.Frequency;
        _lastTimestamp = now;
    }

    private void UpdateRemaining(long now)
    {
        var ticksRemaining = _endTimestamp - now;
        _remaining = ticksRemaining <= 0
            ? 0
            : (int)Math.Ceiling((double)ticksRemaining / Stopwatch.Frequency);
    }

    private void Advance(bool completed)
    {
        if (_mode == Mode.Work)
        {
            if (completed)
                Timeline.Add(new TimelineEntry { Text = $"{DateTime.Now:HH:mm} - Робота ({_phaseSeconds / 60}м)", State = "Done" });
            _mode = _session % _sessionsTotal == 0 ? Mode.LongBreak : Mode.ShortBreak;
        }
        else
        {
            _session = _session % _sessionsTotal + 1;
            _mode = Mode.Work;
        }

        _phaseSeconds = ModeSeconds;
        _remaining = _phaseSeconds;
        RaiseTimer();
    }

    private void RaiseTimer()
    {
        OnPropertyChanged(nameof(TimeText));
        OnPropertyChanged(nameof(Progress));
        OnPropertyChanged(nameof(SessionText));
        OnPropertyChanged(nameof(StartPauseText));
        OnPropertyChanged(nameof(StartPauseGlyph));
    }
}