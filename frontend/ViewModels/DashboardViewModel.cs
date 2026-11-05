using System.Collections.ObjectModel;
using Frontend.Services;

namespace Frontend.ViewModels;

public class DashboardViewModel : PageViewModelBase
{
    public ObservableCollection<DashboardColumn> Columns { get; } =
    [
        new("До виконання",
        [
            new("Особисте", "ВИСОКИЙ", "Спланувати робочий тиждень", "Сьогодні", false, false, 1),
            new("Навчання", "СЕРЕДНІЙ", "Переглянути матеріали курсу", "Завтра", false, false, 0)
        ]),
        new("У процесі",
        [
            new("FocusLab", "ВИСОКИЙ", "Завершити робочий простір", "Сьогодні", false, false, 2)
        ]),
        new("На перевірці",
        [
            new("FocusLab", "НИЗЬКИЙ", "Перевірити макет календаря", "7 листопада", false, false, 0)
        ]),
        new("Відкладені",
        [
            new("Особисте", "СЕРЕДНІЙ", "Розібрати нотатки", "Без терміну", false, false, 0)
        ]),
        new("Завершені",
        [
            new("FocusLab", "НИЗЬКИЙ", "Налаштувати навігацію", "Виконано", true, false, 0)
        ])
    ];

    public int TotalTaskCount => Columns.Sum(column => column.Tasks.Count);

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
}

public class DashboardColumn(string title, IEnumerable<DashboardTask> tasks)
{
    public string Title { get; } = title;
    public ObservableCollection<DashboardTask> Tasks { get; } = new(tasks);
}

public class DashboardTask(
    string project,
    string priority,
    string title,
    string dueDate,
    bool isCompleted,
    bool isOverdue,
    int resourceCount)
{
    public string Project { get; } = project;
    public string Priority { get; } = priority;
    public string Title { get; } = title;
    public string DueDate { get; } = dueDate;
    public bool IsCompleted { get; } = isCompleted;
    public bool IsOverdue { get; } = isOverdue;
    public int ResourceCount { get; } = resourceCount;
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
