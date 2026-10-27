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
            .OrderBy(t => t.CreatedAt)
            .ThenBy(t => t.Id)
            .ToListAsync(ct);
    }

    public async Task<TaskItem?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);

        return await db.Tasks
            .AsNoTracking()
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

        db.Entry(task).State = EntityState.Modified;
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);

        await db.Tasks.Where(t => t.Id == id).ExecuteDeleteAsync(ct);
    }
}
