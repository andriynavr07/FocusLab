using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using FocusLab.Application.Abstractions;
using FocusLab.Application.Dto;
using FocusLab.Domain.Enums;
using Frontend.ViewModels;
using TaskStatus = FocusLab.Domain.Enums.TaskStatus;

namespace Frontend.Services;

/// <summary>
/// Спільна для всіх сторінок колекція задач. Будь-яка зміна рядка (статус, назва, дедлайн…)
/// автоматично зберігається в БД через ITaskService. Операції з БД виконуються строго
/// послідовно (SemaphoreSlim), тому Create завжди завершиться до Update/Delete того ж рядка.
/// </summary>
public sealed class TaskBoardStore
{
    private static readonly HashSet<string> PersistedProperties =
    [
        nameof(TaskRow.Title), nameof(TaskRow.Project), nameof(TaskRow.PriorityKey),
        nameof(TaskRow.Description), nameof(TaskRow.DueDate), nameof(TaskRow.Status)
    ];

    private readonly ITaskService _service;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private bool _isLoading;

    public TaskBoardStore(ITaskService service)
    {
        _service = service;

        Rows.CollectionChanged += (_, e) =>
        {
            if (e.OldItems is not null)
                foreach (TaskRow row in e.OldItems)
                    row.PropertyChanged -= Row_PropertyChanged;

            if (e.NewItems is not null)
                foreach (TaskRow row in e.NewItems)
                {
                    Prepare(row);
                    row.PropertyChanged += Row_PropertyChanged;
                }

            if (!_isLoading)
                Changed?.Invoke(this, EventArgs.Empty);
        };
    }

    public ObservableCollection<TaskRow> Rows { get; } = new();

    public event EventHandler? Changed;

    public async Task LoadAsync()
    {
        _isLoading = true;
        try
        {
            var tasks = await _service.GetAllAsync();
            Rows.Clear();
            foreach (var dto in tasks)
                Rows.Add(FromDto(dto));
        }
        catch (Exception ex)
        {
            ShowError("Не вдалося завантажити задачі", ex);
        }
        finally
        {
            _isLoading = false;
            Changed?.Invoke(this, EventArgs.Empty);
        }
    }

    public void Add(TaskRow row)
    {
        Rows.Add(row);
        Enqueue(async () =>
        {
            var created = await _service.CreateAsync(ToDto(row));
            row.Id = created.Id;

            // CreateAsync завжди ставить Todo; якщо рядок уже має інший статус — дописуємо.
            if (created.Status != ParseStatus(row.Status))
                await _service.UpdateAsync(ToDto(row));
        });
    }

    public void Remove(TaskRow row)
    {
        Rows.Remove(row);
        Enqueue(async () =>
        {
            if (row.Id != 0)
                await _service.DeleteAsync(row.Id);
        });
    }

    public void Prepare(TaskRow row)
        => row.DeleteCommand = new RelayCommand(_ => Remove(row));

    private void Row_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (_isLoading || sender is not TaskRow row)
            return;

        if (e.PropertyName is not null && PersistedProperties.Contains(e.PropertyName))
        {
            Enqueue(async () =>
            {
                // Id читаємо всередині черги: якщо Create ще не завершився, він виконається раніше.
                if (row.Id != 0 && Rows.Contains(row))
                    await _service.UpdateAsync(ToDto(row));
            });
        }

        Changed?.Invoke(this, EventArgs.Empty);
    }

    private async void Enqueue(Func<Task> operation)
    {
        await _gate.WaitAsync();
        try
        {
            await operation();
        }
        catch (Exception ex)
        {
            ShowError("Не вдалося зберегти зміни", ex);
        }
        finally
        {
            _gate.Release();
        }
    }

    // ───── мапінг UI ↔ DTO ─────

    private static TaskRow FromDto(TaskDto dto) => new()
    {
        Id = dto.Id,
        Title = dto.Title,
        Description = dto.Description ?? string.Empty,
        Project = dto.Project ?? string.Empty,
        PriorityKey = dto.Priority switch
        {
            Priority.High or Priority.Critical => "High",
            Priority.Medium => "Medium",
            _ => "Low"
        },
        Status = dto.Status.ToString(),
        // У БД дата в UTC; в UI — локальна дата без часу.
        DueDate = (dto.Deadline ?? dto.CreatedAt).ToLocalTime().Date
    };

    private static TaskDto ToDto(TaskRow row) => new()
    {
        Id = row.Id,
        Title = row.Title,
        Description = string.IsNullOrWhiteSpace(row.Description) ? null : row.Description,
        Project = string.IsNullOrWhiteSpace(row.Project) ? null : row.Project,
        Priority = row.PriorityKey switch
        {
            "High" => Priority.High,
            "Medium" => Priority.Medium,
            _ => Priority.Low
        },
        Status = ParseStatus(row.Status),
        Deadline = DateTime.SpecifyKind(row.DueDate.Date, DateTimeKind.Local)
    };

    private static TaskStatus ParseStatus(string status)
        => Enum.TryParse<TaskStatus>(status, out var parsed) ? parsed : TaskStatus.Todo;

    private static void ShowError(string title, Exception ex)
        => MessageBox.Show($"{title}:\n{ex.Message}", "FocusLab", MessageBoxButton.OK, MessageBoxImage.Error);
}
