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

    public string Title { get; set; } = "";
    public string Project { get; set; } = "";
    public string PriorityKey { get; set; } = "Low"; // High | Medium | Low
    public DateTime DueDate { get; set; } = DateTime.Today;

    public string Status
    {
        get => _status;
        set
        {
            if (!SetField(ref _status, value)) return;
            OnPropertyChanged(nameof(IsDone));
            OnPropertyChanged(nameof(StatusText));
            OnPropertyChanged(nameof(StatusDisplay));
        }
    }

    public bool IsDone
    {
        get => Status == "Done";
        set => Status = value ? "Done" : "Todo";
    }

    public string StatusText => Status switch { "Done" => "Виконано", "InProgress" => "Виконується", _ => "Завдання" };
    public string StatusDisplay => IsDone ? StatusText + " ✓" : StatusText;
    public string PriorityLabel => PriorityKey switch { "High" => "ВАЖЛИВО", "Medium" => "ПОТРІБНО", _ => "НЕОБОВ'ЯЗКОВО" };
    public int PriorityRank => PriorityKey switch { "High" => 0, "Medium" => 1, _ => 2 };
    public string DueText => DueDate.Date == DateTime.Today ? "Сьогодні" : Uk.Short(DueDate);

    public ICommand DeleteCommand { get; set; } = null!;
}

public class TasksViewModel : PageViewModelBase
{
    private string _selStatus = "Усі статуси";
    private string _selProject = "Усі проєкти";
    private string _selPriority = "Усі пріоритети";
    private string _selSort = "Датою виконання";
    private bool _isAddOpen;
    private string _newTitle = "";
    private string _newProject;
    private PriorityOption _newPriority;

    public TasksViewModel()
    {
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
        Rows.CollectionChanged += (_, _) => RaiseList();

        Seed();
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

    public ObservableCollection<TaskRow> Rows { get; } = new();
    public ICollectionView View { get; }

    public string[] StatusFilters { get; } = { "Усі статуси", "Завдання", "Виконується", "Виконано" };
    public string[] ProjectFilters { get; }
    public string[] PriorityFilters { get; } = { "Усі пріоритети", "Важливо", "Потрібно", "Необов'язково" };
    public string[] SortOptions { get; } = { "Датою виконання", "Назвою", "Пріоритетом" };
    public string[] Projects { get; }
    public PriorityOption[] Priorities { get; }

    public string SelectedStatus { get => _selStatus; set { if (SetField(ref _selStatus, value)) View.Refresh(); } }
    public string SelectedProject { get => _selProject; set { if (SetField(ref _selProject, value)) View.Refresh(); } }
    public string SelectedPriority { get => _selPriority; set { if (SetField(ref _selPriority, value)) View.Refresh(); } }
    public string SelectedSort { get => _selSort; set { if (SetField(ref _selSort, value)) ApplySort(); } }

    public bool AllDone => View.Cast<TaskRow>().Any() && View.Cast<TaskRow>().All(r => r.IsDone);
    public bool IsEmpty => View.IsEmpty;

    public bool IsAddOpen { get => _isAddOpen; set => SetField(ref _isAddOpen, value); }
    public string NewTitle { get => _newTitle; set { if (SetField(ref _newTitle, value)) OnPropertyChanged(nameof(CanCreate)); } }
    public string NewProject { get => _newProject; set => SetField(ref _newProject, value); }
    public PriorityOption NewPriority { get => _newPriority; set { if (value != null) SetField(ref _newPriority, value); } }
    public bool CanCreate => NewTitle.Trim().Length is >= 1 and <= 255;

    public ICommand CloseAddCommand { get; }
    public ICommand CreateTaskCommand { get; }
    public ICommand ToggleAllCommand { get; }

    protected override void OnSearchChanged() => View.Refresh();

    private bool Filter(object o)
    {
        var r = (TaskRow)o;
        if (!string.IsNullOrWhiteSpace(SearchText) &&
            !r.Title.Contains(SearchText, StringComparison.OrdinalIgnoreCase)) return false;
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

    private void AddRow(TaskRow row)
    {
        row.DeleteCommand = new RelayCommand(_ => Rows.Remove(row));
        row.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(TaskRow.Status)) { View.Refresh(); RaiseList(); }
        };
        Rows.Add(row);
    }

    private void OpenAdd()
    {
        NewTitle = "";
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
            Project = NewProject,
            PriorityKey = NewPriority.Key,
            DueDate = DateTime.Today
        });
        IsAddOpen = false;
    }

    // Демо-дані → замінити на API
    private void Seed()
    {
        var t = DateTime.Today;
        AddRow(new TaskRow { Title = "Перевірити маркетингові тексти та лендінги", Project = "Launch Plan", PriorityKey = "High", DueDate = t.AddDays(2) });
        AddRow(new TaskRow { Title = "Оптимізувати запити індексів для повільних запитів дашборду", Project = "Backend V2", PriorityKey = "Medium", DueDate = t.AddDays(2) });
        AddRow(new TaskRow { Title = "Скласти специфікацію продукту для оновлень білінгу", Project = "Billing Module", PriorityKey = "Low", DueDate = t.AddDays(2) });
        AddRow(new TaskRow { Title = "Реалізувати прототипи Figma для візуальних стилів таймера", Project = "FocusFlow App", PriorityKey = "High", DueDate = t, Status = "InProgress" });
        AddRow(new TaskRow { Title = "Узгодити з редакцією блог-пост про анонс продукту", Project = "Launch Plan", PriorityKey = "Low", DueDate = t, Status = "InProgress" });
        AddRow(new TaskRow { Title = "Виправити проблеми з cross-origin заголовками", Project = "FocusFlow App", PriorityKey = "High", DueDate = t.AddDays(2), Status = "Done" });
        AddRow(new TaskRow { Title = "Оновити документацію API", Project = "Backend V2", PriorityKey = "Medium", DueDate = t.AddDays(-2), Status = "Done" });
    }
}