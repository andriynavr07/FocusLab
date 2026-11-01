using System.Collections.ObjectModel;
using FocusLab.Application.Abstractions;
using TaskStatus = FocusLab.Domain.Enums.TaskStatus;

namespace FocusLab.App.ViewModels;

public class BoardViewModel : ViewModelBase
{
    private readonly ITaskService _taskService;
    private bool _isLoading;

    public BoardViewModel(ITaskService taskService)
    {
        _taskService = taskService;

        Columns = new ObservableCollection<BoardColumnViewModel>
        {
            new(TaskStatus.Todo, "До виконання"),
            new(TaskStatus.InProgress, "В роботі"),
            new(TaskStatus.Review, "На перевірці"),
            new(TaskStatus.Done, "Виконано")
        };
    }

    public ObservableCollection<BoardColumnViewModel> Columns { get; }

    public bool IsLoading
    {
        get => _isLoading;
        private set => SetProperty(ref _isLoading, value);
    }

    /// <summary>Завантажує задачі з БД і розкладає їх по колонках за статусом.</summary>
    public async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            // Task.Run: доступ до SQLite синхронний під капотом, тож читаємо поза UI-потоком,
            // щоб вікно не "підвисало" під час завантаження.
            var tasks = await Task.Run(() => _taskService.GetAllAsync());

            foreach (var column in Columns)
            {
                column.Tasks.Clear();

                foreach (var task in tasks.Where(t => t.Status == column.Status))
                    column.Tasks.Add(task);
            }
        }
        finally
        {
            IsLoading = false;
        }
    }
}
