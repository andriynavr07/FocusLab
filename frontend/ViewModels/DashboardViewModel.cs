using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Windows.Threading;
using FocusLab.Application.Abstractions;
using FocusLab.Application.Dto;
using Frontend.Services;

namespace Frontend.ViewModels;

public class DashboardViewModel : PageViewModelBase, IDisposable
{
    private readonly TaskBoardStore _store;
    private readonly TimerViewModel _timer;
    private readonly IActivityService _activity;
    private readonly DispatcherTimer _clockTimer;
    private readonly DispatcherTimer _activityDebounce;
    private readonly DashboardStatistic _doneToday = new("Виконано сьогодні", "0 завдань");
    private readonly DashboardStatistic _focusToday = new("Час фокусування", "0 хв");
    private readonly DashboardStatistic _streak = new("Активний серійний період", "0 днів");

    private TaskRow? _selectedTask;
    private bool _isDetailsOpen;
    private string _scheduleDateText = Uk.ShortUpper(DateTime.Today);
    private IReadOnlyList<FocusSessionDto> _sessionsToday = Array.Empty<FocusSessionDto>();
    private int _focusMinutesToday;
    private int _streakDays;
    private bool _activityLoading;
    private bool _activityDirty;
    private bool _disposed;

    public DashboardViewModel(TaskBoardStore store, TimerViewModel timer, IActivityService activity)
    {
        _store = store;
        _timer = timer;
        _activity = activity;
        _store.Changed += OnStoreChanged;
        _timer.FocusSessionFinished += OnFocusSessionFinished;

        CloseDetailsCommand = new RelayCommand(_ => IsDetailsOpen = false);
        StartTaskFocusCommand = new RelayCommand(_ =>
        {
            if (SelectedTask is { IsDone: false })
                SelectedTask.Status = "InProgress";
            _timer.StartFocusFor(SelectedTask);
        });
        ToggleTaskCompleteCommand = new RelayCommand(_ =>
        {
            if (SelectedTask is not null)
                SelectedTask.IsDone = !SelectedTask.IsDone;
        });
        DeleteTaskCommand = new RelayCommand(_ =>
        {
            if (SelectedTask is not null)
            {
                _store.Remove(SelectedTask);
                IsDetailsOpen = false;
            }
        });

        Statistics = new ObservableCollection<DashboardStatistic> { _doneToday, _focusToday, _streak };

        // Дані з БД (фокус-сесії, серійний період) підтягуємо з невеликою затримкою після змін
        // і раз на хвилину — так само оновлюється розклад, дата та «поточний» пункт.
        _activityDebounce = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromMilliseconds(800)
        };
        _activityDebounce.Tick += (_, _) =>
        {
            _activityDebounce.Stop();
            _ = RefreshActivityAsync();
        };

        _clockTimer = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromSeconds(30)
        };
        _clockTimer.Tick += (_, _) =>
        {
            RefreshSchedule();
            RefreshRowStatistics();
            _ = RefreshActivityAsync();
        };
        _clockTimer.Start();

        RefreshBoard();
        RefreshRowStatistics();
        RefreshSchedule();
        _ = RefreshActivityAsync();
    }

    public ObservableCollection<DashboardColumn> Columns { get; } = new();
    public TimerViewModel Timer => _timer;
    public int TotalTaskCount => Columns.Sum(column => column.Tasks.Count);

    public TaskRow? SelectedTask
    {
        get => _selectedTask;
        private set
        {
            SetField(ref _selectedTask, value);
        }
    }

    public bool IsDetailsOpen { get => _isDetailsOpen; set => SetField(ref _isDetailsOpen, value); }
    public string[] TaskStatuses { get; } = { "Todo", "InProgress", "Review", "Deferred", "Done" };

    public ICommand CloseDetailsCommand { get; }
    public ICommand StartTaskFocusCommand { get; }
    public ICommand ToggleTaskCompleteCommand { get; }
    public ICommand DeleteTaskCommand { get; }
    public ICommand ToggleTimerCommand => _timer.ToggleCommand;
    public ICommand ResetTimerCommand => _timer.ResetCommand;
    public ICommand PauseTimerCommand => _timer.PauseCommand;

    /// <summary>«Виконано сьогодні», «Час фокусування», «Активний серійний період» — рахуються з даних.</summary>
    public ObservableCollection<DashboardStatistic> Statistics { get; }

    /// <summary>Розклад на сьогодні: задачі з дедлайном сьогодні + фокус-сесії з БД.</summary>
    public ObservableCollection<DashboardScheduleItem> Schedule { get; } = new();

    public string ScheduleDateText { get => _scheduleDateText; private set => SetField(ref _scheduleDateText, value); }
    public bool IsScheduleEmpty => Schedule.Count == 0;

    protected override void OnSearchChanged() => RefreshBoard();

    // ═════════ Drag-and-drop ═════════

    /// <summary>
    /// Переносить картку в іншу колонку: змінює Status рядка, а TaskBoardStore сам зберігає це в БД
    /// і сповіщає дошку (RefreshBoard перебудовує колонки).
    /// </summary>
    public void MoveTask(DashboardTask card, DashboardColumn target)
    {
        var row = card.Row;
        if (row.Status == target.Status)
            return;

        row.Status = target.Status;
    }

    public void ClearDragHighlight()
    {
        foreach (var column in Columns)
            column.IsDragOver = false;
    }

    // ═════════ Оновлення ═════════

    private void OnStoreChanged(object? sender, EventArgs e)
    {
        RefreshBoard();
        RefreshRowStatistics();
        RefreshSchedule();
        ScheduleActivityRefresh();
    }

    private void OnFocusSessionFinished(object? sender, EventArgs e) => ScheduleActivityRefresh();

    private void ScheduleActivityRefresh()
    {
        if (_disposed) return;
        _activityDebounce.Stop();
        _activityDebounce.Start();
    }

    private void OpenTask(TaskRow row)
    {
        SelectedTask = row;
        IsDetailsOpen = true;
    }

    private void RefreshBoard()
    {
        // Порядок колонок — як на макеті: До виконання → У процесі → На перевірці → Виконано → Відкладені.
        var columns = new[]
        {
            ("До виконання", "Todo"),
            ("У процесі", "InProgress"),
            ("На перевірці", "Review"),
            ("Виконано", "Done"),
            ("Відкладені", "Deferred")
        };

        Columns.Clear();
        foreach (var (title, status) in columns)
        {
            var tasks = _store.Rows
                .Where(row => row.Status == status && MatchesSearch(row))
                .Select(row => new DashboardTask(
                    row,
                    new RelayCommand(_ => OpenTask(row)),
                    StartTaskFocusCommand))
                .ToList();
            Columns.Add(new DashboardColumn(title, status, tasks));
        }

        OnPropertyChanged(nameof(TotalTaskCount));
    }

    /// <summary>Статистика, яку можна порахувати одразу з задач у пам'яті.</summary>
    private void RefreshRowStatistics()
    {
        var today = DateTime.Today;
        var done = _store.Rows.Count(r => r.IsDone && r.CompletedAt?.Date == today);
        _doneToday.Value = $"{done} {Uk.Plural(done, "завдання", "завдання", "завдань")}";
    }

    private async Task RefreshActivityAsync()
    {
        if (_disposed) return;

        // Якщо запит уже виконується, а дані змінилися — повторимо його одразу після завершення.
        if (_activityLoading)
        {
            _activityDirty = true;
            return;
        }

        _activityLoading = true;
        try
        {
            do
            {
                _activityDirty = false;
                var summary = await _activity.GetSummaryAsync();
                if (_disposed) return;

                _sessionsToday = summary.SessionsToday;
                _focusMinutesToday = summary.FocusMinutesToday;
                _streakDays = summary.StreakDays;

                _focusToday.Value = Uk.Duration(_focusMinutesToday);
                _streak.Value = $"{_streakDays} {Uk.Plural(_streakDays, "день", "дні", "днів")}";
                UserSession.Instance.SetStreak(_streakDays);
                RefreshSchedule();
            }
            while (_activityDirty && !_disposed);
        }
        catch
        {
            // Статистика — допоміжна: залишаємо попередні значення, застосунок працює далі.
        }
        finally
        {
            _activityLoading = false;
        }
    }

    private void RefreshSchedule()
    {
        var now = DateTime.Now;
        var today = now.Date;
        ScheduleDateText = Uk.ShortUpper(today);

        var entries = new List<(DateTime Start, DashboardScheduleItem Item)>();

        foreach (var row in _store.Rows.Where(r => r.DueDate.Date == today))
        {
            var hasTime = row.DueDate.TimeOfDay != TimeSpan.Zero;
            var duration = row.EstimatedMinutes is > 0 ? Uk.Duration(row.EstimatedMinutes.Value) : "Без оцінки часу";
            entries.Add((
                row.DueDate,
                new DashboardScheduleItem(hasTime ? row.DueDate.ToString("HH:mm") : "—", row.Title, duration, row.IsDone)));
        }

        foreach (var session in _sessionsToday)
        {
            var start = session.StartUtc.ToLocalTime();
            var title = string.IsNullOrWhiteSpace(session.TaskTitle) ? "Фокус-сесія" : session.TaskTitle;
            var suffix = session.IsCompleted ? "фокусу" : "фокусу (перервано)";
            entries.Add((
                start,
                new DashboardScheduleItem(start.ToString("HH:mm"), title, $"{session.DurationMinutes} хв {suffix}", session.IsCompleted)));
        }

        var ordered = entries.OrderBy(e => e.Start).ToList();

        // Найближчий невиконаний пункт підсвічуємо як «наступний».
        var next = ordered.FirstOrDefault(e => !e.Item.IsDone && e.Start >= now.AddMinutes(-1));
        if (next.Item is not null)
            next.Item.IsNext = true;

        Schedule.Clear();
        foreach (var entry in ordered)
            Schedule.Add(entry.Item);

        OnPropertyChanged(nameof(IsScheduleEmpty));
    }

    private bool MatchesSearch(TaskRow row) =>
        string.IsNullOrWhiteSpace(SearchText)
        || $"{row.Title} {row.Project} {row.Description}".Contains(SearchText, StringComparison.OrdinalIgnoreCase);

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _clockTimer.Stop();
        _activityDebounce.Stop();
        _store.Changed -= OnStoreChanged;
        _timer.FocusSessionFinished -= OnFocusSessionFinished;
    }
}

public class DashboardColumn(string title, string status, IEnumerable<DashboardTask> tasks) : BoardBase
{
    private bool _isDragOver;

    public string Title { get; } = title;

    /// <summary>Значення TaskRow.Status, яке отримає картка після перенесення в цю колонку.</summary>
    public string Status { get; } = status;

    public ObservableCollection<DashboardTask> Tasks { get; } = new(tasks);

    /// <summary>Колонка підсвічується, поки над нею тримають картку.</summary>
    public bool IsDragOver { get => _isDragOver; set => SetField(ref _isDragOver, value); }
}

public class DashboardTask(TaskRow task, ICommand openCommand, ICommand focusCommand)
{
    public TaskRow Row { get; } = task;
    public string Project => task.Project;
    public string Priority => task.PriorityLabel;
    public string Title => task.Title;
    public string DueDate => task.IsDone ? "Виконано" : task.IsOverdue ? "Прострочено" : task.DueText;
    public string EstimateText => task.EstimateText;
    public bool IsCompleted => task.IsDone;
    public bool IsOverdue => task.IsOverdue;
    public int ResourceCount => task.ResourceCount;
    public ICommand OpenCommand { get; } = openCommand;
    public ICommand FocusCommand { get; } = focusCommand;
}

public class DashboardStatistic(string label, string value) : BoardBase
{
    private string _value = value;

    public string Label { get; } = label;
    public string Value { get => _value; set => SetField(ref _value, value); }
}

public class DashboardScheduleItem(string time, string title, string duration, bool isDone) : BoardBase
{
    private bool _isNext;

    public string Time { get; } = time;
    public string Title { get; } = title;
    public string Duration { get; } = duration;
    public bool IsDone { get; } = isDone;
    public bool IsNext { get => _isNext; set => SetField(ref _isNext, value); }
}
