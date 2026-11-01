using FocusLab.Domain.Entities;

namespace FocusLab.Application.Abstractions;

/// <summary>
/// Доступ до задач. Реалізація живе в Infrastructure (EF Core), тому Application
/// не залежить від EF Core, а TaskService можна тестувати без БД.
/// </summary>
public interface ITaskRepository
{
    Task<IReadOnlyList<TaskItem>> GetAllAsync(CancellationToken ct = default);
    Task<TaskItem?> GetByIdAsync(int id, CancellationToken ct = default);
    Task AddAsync(TaskItem task, CancellationToken ct = default);
    Task UpdateAsync(TaskItem task, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
}
