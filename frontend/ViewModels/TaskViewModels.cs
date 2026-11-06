using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using System.Windows.Input;
using Frontend.Services;

namespace Frontend.ViewModels;

public class PriorityOption(string key, string label)
{
    public string Key { get; } = key;
    public string Label { get; } = label;
}

/// <summary>Варіант списку «Оцінка часу».</summary>
public class EstimateOption(int minutes)
{
    public int Minutes { get; } = minutes;
    public string Label { get; } = Uk.Duration(minutes);
}

public class TaskRow : BoardBase
{
    private string _status = "Todo";
    private string _title = "";
    private string _project = "";
    private string _priorityKey = "Low";
    private string _description = "";
    private DateTime _dueDate = DateTime.Today;
    private int _resourceCount;
    private int _id;
    private int? _estimatedMinutes;
    private DateTime? _completedAt;

    /// <summary>Id задачі в БД; 0 — ще не збережено.</summary>
    public int Id { get => _id; set => SetField(ref _id, value); }
    public string Title { get => _title; set => SetField(ref _title, value); }
    public string Project { get => _project; set => SetField(ref _project, value); }
    public string PriorityKey
    {
        get => _priorityKey;
        set
        {
            if (!SetField(ref _priorityKey, value)) return;
            OnPropertyChanged(nameof(PriorityLabel));
            OnPropertyChanged(nameof(PriorityRank));
        }
    }
    public string Description { get => _description; set => SetField(ref _description, value); }
    public DateTime DueDate
    {
        get => _dueDate;
        set
        {
            if (!SetField(ref _dueDate, value)) return;
            OnPropertyChanged(nameof(DueText));
            OnPropertyChanged(nameof(IsOverdue));
        }
    }
    public int ResourceCount { get => _resourceCount; set => SetField(ref _resourceCount, value); }

    /// <summary>Оцінка часу, хв (з форми «Нове завдання»).</summary>
    public int? EstimatedMinutes
    {
        get => _estimatedMinutes;
        set
        {
            if (!SetField(ref _estimatedMinutes, value)) return;
            OnPropertyChanged(nameof(EstimateText));
        }
    }

    /// <summary>Локальний час виконання; потрібен для статистики «виконано сьогодні» та серійного періоду.</summary>
    public DateTime? CompletedAt { get => _completedAt; set => SetField(ref _completedAt, value); }

    public string Status
    {
        get => _status;
        set
        {
            if (!SetField(ref _status, value)) return;
            CompletedAt = value == "Done" ? DateTime.Now : null;
            OnPropertyChanged(nameof(IsDone));
            OnPropertyChanged(nameof(StatusText));
            OnPropertyChanged(nameof(StatusDisplay));
            OnPropertyChanged(nameof(IsOverdue));
        }
    }

    public bool IsDone
    {
        get => Status == "Done";
        set => Status = value ? "Done" : "Todo";
    }

    public string StatusText => Status switch
    {
        "Done" => "Виконано",
        "InProgress" => "Виконується",
        "Review" => "На перевірці",
        "Deferred" => "Відкладено",
        _ => "Завдання"
    };
    public string StatusDisplay => IsDone ? StatusText + " ✓" : StatusText;
    public string PriorityLabel => PriorityKey switch { "Critical" => "КРИТИЧНИЙ", "High" => "ВИСОКИЙ", "Medium" => "СЕРЕДНІЙ", _ => "НИЗЬКИЙ" };
    public int PriorityRank => PriorityKey switch { "Critical" => 0, "High" => 1, "Medium" => 2, _ => 3 };
    public string EstimateText => EstimatedMinutes is > 0 ? Uk.Duration(EstimatedMinutes.Value).ToUpperInvariant() : string.Empty;
    public string DueText => DueDate.Date == DateTime.Today ? "Сьогодні" : Uk.Short(DueDate);
    public bool IsOverdue => !IsDone && DueDate.Date < DateTime.Today;

    public ICommand DeleteCommand { get; set; } = null!;
}

public class TasksViewModel : PageViewModelBase
{
    private readonly TaskBoardStore _store;
    private string _selStatus = "Усі статуси";
    private string _selProject = "Усі проєкти";
    private string _selPriority = "Усі пріоритети";
    private string _selSort = "Датою виконання";
    private bool _isAddOpen;
    private string _newTitle = "";
    private string _newDescription = "";
    private DateTime _newDueDate = DateTime.Today;
    private int _newDueHour = 18;
    private int _newDueMinute;
    private bool _isDatePickerOpen;
    private string _newProject;
    private PriorityOption _newPriority;
    private EstimateOption _newEstimate;

    public TasksViewModel(TaskBoardStore store)
    {
        _store = store;
        Projects = new[] { "Launch Plan", "Backend V2", "Billing Module", "FocusFlow App" };
        ProjectFilters = new[] { "Усі проєкти" }.Concat(Projects).ToArray();
        ProjectChoices = new[] { NoProject }.Concat(Projects).ToArray();
        // Порядок і назви — як на макеті модалки «Нове завдання».
        Priorities = new[]
        {
            new PriorityOption("Medium", "Середній"),
            new PriorityOption("Low", "Низький"),
            new PriorityOption("High", "Високий"),
            new PriorityOption("Critical", "Критичний")
        };
        EstimateOptions = new[] { 15, 25, 30, 45, 60, 90, 120, 180 }.Select(m => new EstimateOption(m)).ToArray();
        _newProject = NoProject;
        _newPriority = Priorities[0];
        _newEstimate = EstimateOptions.First(o => o.Minutes == 60);
        Hours = Enumerable.Range(0, 24).ToArray();
        Minutes = new[] { 0, 5, 10, 15, 20, 25, 30, 35, 40, 45, 50, 55 };

        View = new CollectionViewSource { Source = Rows }.View;
        View.Filter = Filter;
        _store.Changed += (_, _) =>
            System.Windows.Application.Current.Dispatcher.InvokeAsync(RefreshView);
        ApplySort();

        OpenNewTaskCommand = new RelayCommand(_ => OpenAdd());
        CloseAddCommand = new RelayCommand(_ => { IsDatePickerOpen = false; IsAddOpen = false; });
        SelectPriorityCommand = new RelayCommand(p => { if (p is PriorityOption o) NewPriority = o; });
        CreateTaskCommand = new RelayCommand(_ => Create());
        ToggleAllCommand = new RelayCommand(_ =>
        {
            var target = !AllDone;
            foreach (var r in View.Cast<TaskRow>().ToList()) r.IsDone = target;
        });
    }

    public ObservableCollection<TaskRow> Rows => _store.Rows;
    public ICollectionView View { get; }

    public string[] StatusFilters { get; } = { "Усі статуси", "Завдання", "Виконується", "На перевірці", "Відкладено", "Виконано" };
    public string[] ProjectFilters { get; }
    public string[] PriorityFilters { get; } = { "Усі пріоритети", "Критичний", "Високий", "Середній", "Низький" };
    public string[] SortOptions { get; } = { "Датою виконання", "Назвою", "Пріоритетом" };
    public string[] Projects { get; }

    /// <summary>Значення «Без проєкту» у випадному списку форми створення.</summary>
    public const string NoProject = "Без проєкту";
    public string[] ProjectChoices { get; }
    public PriorityOption[] Priorities { get; }
    public EstimateOption[] EstimateOptions { get; }
    public int[] Hours { get; }
    public int[] Minutes { get; }

    public string SelectedStatus { get => _selStatus; set { if (SetField(ref _selStatus, value)) RefreshView(); } }
    public string SelectedProject { get => _selProject; set { if (SetField(ref _selProject, value)) RefreshView(); } }
    public string SelectedPriority { get => _selPriority; set { if (SetField(ref _selPriority, value)) RefreshView(); } }
    public string SelectedSort { get => _selSort; set { if (SetField(ref _selSort, value)) ApplySort(); } }

    public bool AllDone => View.Cast<TaskRow>().Any() && View.Cast<TaskRow>().All(r => r.IsDone);
    public bool IsEmpty => View.IsEmpty;

    public bool IsAddOpen { get => _isAddOpen; set => SetField(ref _isAddOpen, value); }
    public string NewTitle { get => _newTitle; set { if (SetField(ref _newTitle, value)) OnPropertyChanged(nameof(CanCreate)); } }
    public string NewDescription { get => _newDescription; set => SetField(ref _newDescription, value); }
    public DateTime NewDueDate
    {
        get => _newDueDate;
        set { if (SetField(ref _newDueDate, value.Date)) OnPropertyChanged(nameof(NewDueText)); }
    }

    /// <summary>Проксі для Calendar.SelectedDate (він nullable): повторний клік по даті не скидає вибір.</summary>
    public DateTime? NewDueDatePick
    {
        get => _newDueDate;
        set
        {
            if (value.HasValue)
            {
                NewDueDate = value.Value;
                IsDatePickerOpen = false;
            }
            OnPropertyChanged();
        }
    }

    public int NewDueHour { get => _newDueHour; set { if (SetField(ref _newDueHour, value)) OnPropertyChanged(nameof(NewDueText)); } }
    public int NewDueMinute { get => _newDueMinute; set { if (SetField(ref _newDueMinute, value)) OnPropertyChanged(nameof(NewDueText)); } }
    public bool IsDatePickerOpen { get => _isDatePickerOpen; set => SetField(ref _isDatePickerOpen, value); }

    /// <summary>Повний термін (дата + час) із форми.</summary>
    public DateTime NewDueDateTime => _newDueDate.Date.AddHours(_newDueHour).AddMinutes(_newDueMinute);

    /// <summary>«22 ВЕР 2026 · 18:00».</summary>
    public string NewDueText => $"{Uk.ShortUpper(NewDueDate, withYear: true)} · {NewDueHour:00}:{NewDueMinute:00}";

    public string NewProject { get => _newProject; set => SetField(ref _newProject, value); }
    public PriorityOption NewPriority { get => _newPriority; set { if (value != null) SetField(ref _newPriority, value); } }
    public EstimateOption NewEstimate { get => _newEstimate; set { if (value != null) SetField(ref _newEstimate, value); } }
    public bool CanCreate => NewTitle.Trim().Length is >= 1 and <= 255;

    public ICommand CloseAddCommand { get; }
    public ICommand SelectPriorityCommand { get; }
    public ICommand CreateTaskCommand { get; }
    public ICommand ToggleAllCommand { get; }

    protected override void OnSearchChanged() => RefreshView();

    private void RefreshView()
    {
        View.Refresh();
        RaiseList();
    }

    private bool Filter(object o)
    {
        var r = (TaskRow)o;
        if (!string.IsNullOrWhiteSpace(SearchText) &&
            !($"{r.Title} {r.Project} {r.Description}".Contains(SearchText, StringComparison.OrdinalIgnoreCase))) return false;
        if (SelectedStatus != StatusFilters[0] && r.StatusText != SelectedStatus) return false;
        if (SelectedProject != ProjectFilters[0] && r.Project != SelectedProject) return false;
        if (SelectedPriority != PriorityFilters[0] &&
            !r.PriorityLabel.Equals(SelectedPriority, StringComparison.OrdinalIgnoreCase)) return false;
        return true;
    }

    private void ApplySort()
    {
        using (View.DeferRefresh())
        {
            View.SortDescriptions.Clear();
            View.SortDescriptions.Add(new SortDescription(nameof(TaskRow.IsDone), ListSortDirection.Ascending));
            var key = SelectedSort switch
            {
                "Назвою" => nameof(TaskRow.Title),
                "Пріоритетом" => nameof(TaskRow.PriorityRank),
                _ => nameof(TaskRow.DueDate)
            };
            View.SortDescriptions.Add(new SortDescription(key, ListSortDirection.Ascending));
        }
        RaiseList();
    }

    private void RaiseList()
    {
        OnPropertyChanged(nameof(IsEmpty));
        OnPropertyChanged(nameof(AllDone));
    }

    private void AddRow(TaskRow row) => _store.Add(row);

    private void OpenAdd()
    {
        NewTitle = "";
        NewDescription = "";
        // За замовчуванням — сьогодні о 18:00 (якщо вже пізніше — наступна повна година).
        var due = DateTime.Today.AddHours(18);
        if (due <= DateTime.Now)
            due = DateTime.Today.AddHours(DateTime.Now.Hour + 1);
        NewDueDate = due.Date;
        NewDueHour = due.Hour;
        NewDueMinute = 0;
        NewProject = NoProject;
        NewPriority = Priorities[0];
        NewEstimate = EstimateOptions.First(o => o.Minutes == 60);
        IsDatePickerOpen = false;
        IsAddOpen = true;
    }

    private void Create()
    {
        if (!CanCreate) return;
        AddRow(new TaskRow
        {
            Title = NewTitle.Trim(),
            Description = NewDescription.Trim(),
            Project = NewProject == NoProject ? string.Empty : NewProject,
            PriorityKey = NewPriority.Key,
            DueDate = NewDueDateTime,
            EstimatedMinutes = NewEstimate.Minutes
        });
        IsDatePickerOpen = false;
        IsAddOpen = false;
    }

}