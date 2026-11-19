using FocusLab.Application.Abstractions;
using FocusLab.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FocusLab.Infrastructure.Persistence;

public class TaskRepository : ITaskRepository
{
    private readonly IDbContextFactory<AppDbContext> _factory;

    public TaskRepository(IDbContextFactory<AppDbContext> factory)
    {
        _factory = factory;
    }

    public async Task<IReadOnlyList<TaskItem>> GetAllAsync(CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);

        return await db.Tasks
            .AsNoTracking()
            .Include(task => task.TaskTags)
            .OrderBy(t => t.CreatedAt)
            .ThenBy(t => t.Id)
            .ToListAsync(ct);
    }

    public async Task<TaskItem?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);

        return await db.Tasks
            .AsNoTracking()
            .Include(task => task.TaskTags)
            .FirstOrDefaultAsync(t => t.Id == id, ct);
    }

    public async Task AddAsync(TaskItem task, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);

        db.Tasks.Add(task);
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(TaskItem task, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);

        var existing = await db.Tasks.FirstOrDefaultAsync(item => item.Id == task.Id, ct)
            ?? throw new KeyNotFoundException($"Задачу з Id={task.Id} не знайдено.");
        existing.AccountId = task.AccountId;
        existing.Title = task.Title;
        existing.Description = task.Description;
        existing.Project = task.Project;
        existing.Deadline = task.Deadline;
        existing.EstimatedMinutes = task.EstimatedMinutes;
        existing.Priority = task.Priority;
        existing.Status = task.Status;
        existing.CreatedAt = task.CreatedAt;
        existing.CompletedAt = task.CompletedAt;
        existing.ChecklistJson = task.ChecklistJson;
        await db.SaveChangesAsync(ct);
    }

    public async Task SetTagsAsync(int taskId, IReadOnlyCollection<int> tagIds, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        var task = await db.Tasks.Include(item => item.TaskTags)
            .FirstOrDefaultAsync(item => item.Id == taskId, ct)
            ?? throw new KeyNotFoundException($"Задачу з Id={taskId} не знайдено.");
        var distinctIds = tagIds.Distinct().ToArray();
        var existingIds = await db.Tags
            .Where(tag => tag.AccountId == task.AccountId && distinctIds.Contains(tag.Id))
            .Select(tag => tag.Id)
            .ToListAsync(ct);
        if (existingIds.Count != distinctIds.Length)
            throw new ArgumentException("Один або кілька тегів не знайдені.", nameof(tagIds));

        db.TaskTags.RemoveRange(task.TaskTags);
        foreach (var tagId in distinctIds)
            db.TaskTags.Add(new TaskTag { TaskId = taskId, TagId = tagId });
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);

        await db.Tasks.Where(t => t.Id == id).ExecuteDeleteAsync(ct);
    }
}
