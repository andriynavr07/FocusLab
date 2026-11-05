using System.Collections.ObjectModel;
using System.Windows.Input;
using Frontend.Services;

namespace Frontend.ViewModels;

public class DashboardViewModel : PageViewModelBase
{
    private readonly TaskBoardStore _store;
    private readonly TimerViewModel _timer;
    private TaskRow? _selectedTask;
    private bool _isDetailsOpen;

    public DashboardViewModel(TaskBoardStore store, TimerViewModel timer)
    {
        _store = store;
        _timer = timer;
        _store.Changed += (_, _) => RefreshBoard();

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

        RefreshBoard();
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

    public ObservableCollection<DashboardStatistic> Statistics { get; } =
    [
        new("Виконано сьогодні", "3 завдання"),
        new("Час фокусування", "1 год 25 хв"),
        new("Активний серійний період", "5 днів")
    ];

    public ObservableCollection<DashboardScheduleItem> Schedule { get; } =
    [
        new("09:00", "Планування дня", "15 хв"),
        new("10:00", "Робота над FocusLab", "25 хв фокусу"),
        new("11:00", "Перерва", "10 хв")
    ];

    protected override void OnSearchChanged() => RefreshBoard();

    private void OpenTask(TaskRow row)
    {
        SelectedTask = row;
        IsDetailsOpen = true;
    }

    private void RefreshBoard()
    {
        var columns = new[]
        {
            ("До виконання", "Todo"),
            ("У процесі", "InProgress"),
            ("На перевірці", "Review"),
            ("Відкладені", "Deferred"),
            ("Завершені", "Done")
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
            Columns.Add(new DashboardColumn(title, tasks));
        }

        OnPropertyChanged(nameof(TotalTaskCount));
    }

    private bool MatchesSearch(TaskRow row) =>
        string.IsNullOrWhiteSpace(SearchText)
        || $"{row.Title} {row.Project} {row.Description}".Contains(SearchText, StringComparison.OrdinalIgnoreCase);
}

public class DashboardColumn(string title, IEnumerable<DashboardTask> tasks)
{
    public string Title { get; } = title;
    public ObservableCollection<DashboardTask> Tasks { get; } = new(tasks);
}

public class DashboardTask(TaskRow task, ICommand openCommand, ICommand focusCommand)
{
    public string Project => task.Project;
    public string Priority => task.PriorityKey switch { "High" => "ВИСОКИЙ", "Medium" => "СЕРЕДНІЙ", _ => "НИЗЬКИЙ" };
    public string Title => task.Title;
    public string DueDate => task.IsDone ? "Виконано" : task.IsOverdue ? "Прострочено" : task.DueText;
    public bool IsCompleted => task.IsDone;
    public bool IsOverdue => task.IsOverdue;
    public int ResourceCount => task.ResourceCount;
    public ICommand OpenCommand { get; } = openCommand;
    public ICommand FocusCommand { get; } = focusCommand;
}

public class DashboardStatistic(string label, string value)
{
    public string Label { get; } = label;
    public string Value { get; } = value;
}

public class DashboardScheduleItem(string time, string title, string duration)
{
    public string Time { get; } = time;
    public string Title { get; } = title;
    public string Duration { get; } = duration;
}
