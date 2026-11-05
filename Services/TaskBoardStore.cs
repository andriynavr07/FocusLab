using System.Collections.ObjectModel;
using System.ComponentModel;
using Frontend.ViewModels;
using System.Windows.Input;

namespace Frontend.Services;

public sealed class TaskBoardStore
{
    public TaskBoardStore()
    {
        Rows.CollectionChanged += (_, e) =>
        {
            if (e.OldItems is not null)
                foreach (TaskRow row in e.OldItems)
                    row.PropertyChanged -= Row_PropertyChanged;

            if (e.NewItems is not null)
                foreach (TaskRow row in e.NewItems)
                    row.PropertyChanged += Row_PropertyChanged;

            Changed?.Invoke(this, EventArgs.Empty);
        };

        var today = DateTime.Today;
        Rows.Add(new TaskRow { Title = "Перевірити маркетингові тексти та лендінги", Project = "Launch Plan", PriorityKey = "High", DueDate = today.AddDays(2), Description = "Перевірити тексти перед запуском." });
        Rows.Add(new TaskRow { Title = "Оптимізувати запити індексів для повільних запитів дашборду", Project = "Backend V2", PriorityKey = "Medium", DueDate = today.AddDays(2) });
        Rows.Add(new TaskRow { Title = "Скласти специфікацію продукту для оновлень білінгу", Project = "Billing Module", PriorityKey = "Low", DueDate = today.AddDays(2) });
        Rows.Add(new TaskRow { Title = "Реалізувати прототипи Figma для візуальних стилів таймера", Project = "FocusFlow App", PriorityKey = "High", DueDate = today, Status = "InProgress" });
        Rows.Add(new TaskRow { Title = "Узгодити з редакцією блог-пост про анонс продукту", Project = "Launch Plan", PriorityKey = "Low", DueDate = today, Status = "InProgress" });
        Rows.Add(new TaskRow { Title = "Виправити проблеми з cross-origin заголовками", Project = "FocusFlow App", PriorityKey = "High", DueDate = today.AddDays(2), Status = "Done" });
        Rows.Add(new TaskRow { Title = "Оновити документацію API", Project = "Backend V2", PriorityKey = "Medium", DueDate = today.AddDays(-2), Status = "Done" });
    }

    public ObservableCollection<TaskRow> Rows { get; } = new();

    public event EventHandler? Changed;

    public void Add(TaskRow row) => Rows.Add(row);

    public void Remove(TaskRow row) => Rows.Remove(row);

    public void Prepare(TaskRow row)
        => row.DeleteCommand = new RelayCommand(_ => Remove(row));

    private void Row_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        => Changed?.Invoke(this, EventArgs.Empty);
}
