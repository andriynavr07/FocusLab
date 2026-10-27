using FocusLab.Application.Abstractions;
using FocusLab.Application.Dto;
using FocusLab.Domain.Entities;
using TaskStatus = FocusLab.Domain.Enums.TaskStatus;

namespace FocusLab.Application.Services;

public class TaskService : ITaskService
{
    public const int MaxTitleLength = 255;

    private readonly ITaskRepository _repository;
    private readonly IClock _clock;

    public TaskService(ITaskRepository repository, IClock clock)
    {
        _repository = repository;
        _clock = clock;
    }

    public async Task<IReadOnlyList<TaskDto>> GetAllAsync(CancellationToken ct = default)
    {
        var items = await _repository.GetAllAsync(ct);
        return items.Select(ToDto).ToList();
    }

    public async Task<TaskDto> CreateAsync(TaskDto task, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(task);

        var entity = new TaskItem
        {
            AccountId = task.AccountId,
            Title = ValidateTitle(task.Title),
            Description = task.Description,
            Deadline = task.Deadline,
            Priority = task.Priority,
            Status = TaskStatus.Todo,          // нова задача завжди починає з Todo
            CreatedAt = _clock.UtcNow          // час береться з IClock, а не з DateTime.UtcNow
        };

        await _repository.AddAsync(entity, ct);
        return ToDto(entity);
    }

    public async Task<TaskDto> UpdateAsync(TaskDto task, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(task);

        var entity = await _repository.GetByIdAsync(task.Id, ct)
            ?? throw new KeyNotFoundException($"Задачу з Id={task.Id} не знайдено.");

        entity.Title = ValidateTitle(task.Title);
        entity.Description = task.Description;
        entity.Deadline = task.Deadline;
        entity.Priority = task.Priority;

        if (task.Status != entity.Status)
        {
            entity.Status = task.Status;
            entity.CompletedAt = task.Status == TaskStatus.Done ? _clock.UtcNow : null;
        }

        await _repository.UpdateAsync(entity, ct);
        return ToDto(entity);
    }

    public Task DeleteAsync(int id, CancellationToken ct = default)
        => _repository.DeleteAsync(id, ct);

    private static string ValidateTitle(string? title)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Назва задачі не може бути порожньою.", nameof(title));

        var trimmed = title.Trim();
        if (trimmed.Length > MaxTitleLength)
            throw new ArgumentException($"Назва задачі не може бути довшою за {MaxTitleLength} символів.", nameof(title));

        return trimmed;
    }

    private static TaskDto ToDto(TaskItem t) => new()
    {
        Id = t.Id,
        AccountId = t.AccountId,
        Title = t.Title,
        Description = t.Description,
        Deadline = t.Deadline,
        Priority = t.Priority,
        Status = t.Status,
        CreatedAt = t.CreatedAt,
        CompletedAt = t.CompletedAt
    };
}
