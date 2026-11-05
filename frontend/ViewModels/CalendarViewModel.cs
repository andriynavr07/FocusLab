using System.Collections.ObjectModel;
using System.Windows.Input;
using Frontend.Services;

namespace Frontend.ViewModels;

public class CalendarEvent
{
    public string Title { get; init; } = "";
    public TimeSpan Start { get; init; }
    public int Minutes { get; init; }
    public string Project { get; init; } = "";
    public string Kind { get; init; } = "Accent"; // Accent | Success | Danger
    public bool IsDone { get; init; }

    public string TimeRange => $"{Start:hh\\:mm} - {Start.Add(TimeSpan.FromMinutes(Minutes)):hh\\:mm}";
    public string Meta => $"{Duration} • {Project}";

    private string Duration => Minutes % 60 == 0
        ? $"{Minutes / 60} {Uk.Plural(Minutes / 60, "година", "години", "годин")}"
        : $"{Minutes} хвилин";
}

public class DayCell : BoardBase
{
    private bool _isSelected;
    public DateTime? Date { get; init; }
    public string Number => Date?.Day.ToString() ?? "";
    public bool HasDate => Date.HasValue;
    public bool IsToday => Date?.Date == DateTime.Today;
    public List<string> Dots { get; init; } = new();
    public bool IsSelected { get => _isSelected; set => SetField(ref _isSelected, value); }
    public ICommand SelectCommand { get; set; } = null!;
}

public class CalendarViewModel : PageViewModelBase
{
    private static readonly string[] MonthNames =
    {
        "Січень", "Лютий", "Березень", "Квітень", "Травень", "Червень",
        "Липень", "Серпень", "Вересень", "Жовтень", "Листопад", "Грудень"
    };

    private static readonly string[] DayNames =
        { "Понеділок", "Вівторок", "Середа", "Четвер", "П’ятниця", "Субота", "Неділя" };

    private readonly TaskBoardStore _store;
    private DateTime _month = new(DateTime.Today.Year, DateTime.Today.Month, 1);
    private DateTime _selected = DateTime.Today;

    public CalendarViewModel(TaskBoardStore store)
    {
        _store = store;
        PrevMonthCommand = new RelayCommand(_ => ChangeMonth(-1));
        NextMonthCommand = new RelayCommand(_ => ChangeMonth(1));
        _store.Changed += (_, _) =>
        {
            Build();
            LoadSelected();
            OnPropertyChanged(nameof(MonthTasksText));
            OnPropertyChanged(nameof(DonePercentText));
        };
        Build();
        LoadSelected();
    }

    public ObservableCollection<DayCell> Days { get; } = new();
    public ObservableCollection<CalendarEvent> SelectedEvents { get; } = new();

    public ICommand PrevMonthCommand { get; }
    public ICommand NextMonthCommand { get; }

    public string MonthTitle => $"{MonthNames[_month.Month - 1]} {_month.Year}";

    public string SelectedDayTitle
    {
        get
        {
            var m = Uk.Month(_selected);
            var month = char.ToUpper(m[0]) + m[1..].ToLower();
            return $"{DayNames[((int)_selected.DayOfWeek + 6) % 7]}, {_selected.Day} {month}";
        }
    }

    public string MonthTasksText
    {
        get
        {
            var n = MonthEvents().Count();
            return $"{n} {Uk.Plural(n, "завдання", "завдання", "завдань")}";
        }
    }

    public string DonePercentText
    {
        get
        {
            var all = _store.Rows
                .Where(row => row.DueDate.Year == _month.Year && row.DueDate.Month == _month.Month)
                .ToList();
            if (all.Count == 0) return "0%";
            var done = all.Count(row => row.IsDone);
            return $"{Math.Round(100.0 * done / all.Count)}%";
        }
    }

    public bool HasNoEvents => SelectedEvents.Count == 0;

    public string AdviceText
    {
        get
        {
            var n = SelectedEvents.Count;
            return n == 0
                ? "Вільний день. Використайте його для планування або відпочинку."
                : $"На цей день заплановано {n} {Uk.Plural(n, "завдання", "завдання", "завдань")}. " +
                  "Рекомендуємо зробити довгу перерву після другої сесії.";
        }
    }

    protected override void OnSearchChanged() => LoadSelected();

    private IEnumerable<(DateTime date, CalendarEvent ev)> MonthEventsRaw() =>
        _store.Rows
            .Where(row => row.DueDate.Year == _month.Year && row.DueDate.Month == _month.Month)
            .OrderBy(row => row.DueDate)
            .ThenBy(row => row.Title)
            .Select((row, index) => (row.DueDate.Date, CreateEvent(row, index)));

    private IEnumerable<(DateTime date, CalendarEvent ev)> MonthEvents() => MonthEventsRaw();

    private void ChangeMonth(int delta)
    {
        _month = _month.AddMonths(delta);
        _selected = _month.Year == DateTime.Today.Year && _month.Month == DateTime.Today.Month
            ? DateTime.Today
            : _month;
        Build();
        LoadSelected();
        OnPropertyChanged(nameof(MonthTitle));
        OnPropertyChanged(nameof(MonthTasksText));
        OnPropertyChanged(nameof(DonePercentText));
    }

    private void Build()
    {
        Days.Clear();
        var offset = ((int)_month.DayOfWeek + 6) % 7; // понеділок = 0
        for (var i = 0; i < offset; i++) Days.Add(new DayCell());

        for (var d = 1; d <= DateTime.DaysInMonth(_month.Year, _month.Month); d++)
        {
            var date = new DateTime(_month.Year, _month.Month, d);
            var cell = new DayCell
            {
                Date = date,
                IsSelected = date == _selected.Date,
                Dots = EventsFor(date).Take(3).Select(e => e.Kind).ToList()
            };
            cell.SelectCommand = new RelayCommand(_ => Select(cell));
            Days.Add(cell);
        }

        while (Days.Count % 7 != 0) Days.Add(new DayCell());
    }

    private void Select(DayCell cell)
    {
        if (cell.Date is null) return;
        _selected = cell.Date.Value;
        foreach (var d in Days) d.IsSelected = ReferenceEquals(d, cell);
        LoadSelected();
    }

    private void LoadSelected()
    {
        SelectedEvents.Clear();
        foreach (var e in EventsFor(_selected))
        {
            if (!string.IsNullOrWhiteSpace(SearchText) &&
                !($"{e.Title} {e.Project}".Contains(SearchText, StringComparison.OrdinalIgnoreCase))) continue;
            SelectedEvents.Add(e);
        }

        OnPropertyChanged(nameof(SelectedDayTitle));
        OnPropertyChanged(nameof(HasNoEvents));
        OnPropertyChanged(nameof(AdviceText));
    }

    private List<CalendarEvent> EventsFor(DateTime date) =>
        _store.Rows
            .Where(row => row.DueDate.Date == date.Date)
            .OrderBy(row => row.Title)
            .Select((row, index) => CreateEvent(row, index))
            .ToList();

    private static CalendarEvent CreateEvent(TaskRow row, int index)
    {
        var startHour = 9 + index;
        return new CalendarEvent
        {
            Title = row.Title,
            Start = new TimeSpan(startHour, 0, 0),
            Minutes = 25,
            Project = row.Project,
            Kind = row.IsDone ? "Success" : row.IsOverdue ? "Danger" : "Accent",
            IsDone = row.IsDone
        };
    }
}