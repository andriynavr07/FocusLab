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

    public string Status
    {
        get => _status;
        set
        {
            if (!SetField(ref _status, value)) return;
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
    public string PriorityLabel => PriorityKey switch { "High" => "ВАЖЛИВО", "Medium" => "ПОТРІБНО", _ => "НЕОБОВ'ЯЗКОВО" };
    public int PriorityRank => PriorityKey switch { "High" => 0, "Medium" => 1, _ => 2 };
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
    private string _newProject;
    private PriorityOption _newPriority;

    public TasksViewModel(TaskBoardStore store)
    {
        _store = store;
        Projects = new[] { "Launch Plan", "Backend V2", "Billing Module", "FocusFlow App" };
        ProjectFilters = new[] { "Усі проєкти" }.Concat(Projects).ToArray();
        Priorities = new[]
        {
            new PriorityOption("High", "Важливо"),
            new PriorityOption("Medium", "Потрібно"),
            new PriorityOption("Low", "Необов'язково")
        };
        _newProject = Projects[0];
        _newPriority = Priorities[1];

        View = new CollectionViewSource { Source = Rows }.View;
        View.Filter = Filter;
        _store.Changed += (_, _) =>
            System.Windows.Application.Current.Dispatcher.InvokeAsync(RefreshView);
        ApplySort();

        OpenNewTaskCommand = new RelayCommand(_ => OpenAdd());
        CloseAddCommand = new RelayCommand(_ => IsAddOpen = false);
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
    public string[] PriorityFilters { get; } = { "Усі пріоритети", "Важливо", "Потрібно", "Необов'язково" };
    public string[] SortOptions { get; } = { "Датою виконання", "Назвою", "Пріоритетом" };
    public string[] Projects { get; }
    public PriorityOption[] Priorities { get; }

    public string SelectedStatus { get => _selStatus; set { if (SetField(ref _selStatus, value)) RefreshView(); } }
    public string SelectedProject { get => _selProject; set { if (SetField(ref _selProject, value)) RefreshView(); } }
    public string SelectedPriority { get => _selPriority; set { if (SetField(ref _selPriority, value)) RefreshView(); } }
    public string SelectedSort { get => _selSort; set { if (SetField(ref _selSort, value)) ApplySort(); } }

    public bool AllDone => View.Cast<TaskRow>().Any() && View.Cast<TaskRow>().All(r => r.IsDone);
    public bool IsEmpty => View.IsEmpty;

    public bool IsAddOpen { get => _isAddOpen; set => SetField(ref _isAddOpen, value); }
    public string NewTitle { get => _newTitle; set { if (SetField(ref _newTitle, value)) OnPropertyChanged(nameof(CanCreate)); } }
    public string NewDescription { get => _newDescription; set => SetField(ref _newDescription, value); }
    public DateTime NewDueDate { get => _newDueDate; set => SetField(ref _newDueDate, value); }
    public string NewProject { get => _newProject; set => SetField(ref _newProject, value); }
    public PriorityOption NewPriority { get => _newPriority; set { if (value != null) SetField(ref _newPriority, value); } }
    public bool CanCreate => NewTitle.Trim().Length is >= 1 and <= 255;

    public ICommand CloseAddCommand { get; }
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
        NewDueDate = DateTime.Today;
        NewProject = Projects[0];
        NewPriority = Priorities[1];
        IsAddOpen = true;
    }

    private void Create()
    {
        if (!CanCreate) return;
        AddRow(new TaskRow
        {
            Title = NewTitle.Trim(),
            Description = NewDescription.Trim(),
            Project = NewProject,
            PriorityKey = NewPriority.Key,
            DueDate = NewDueDate
        });
        IsAddOpen = false;
    }

}