using FocusLab.Application.Abstractions;
using FocusLab.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FocusLab.Infrastructure.Persistence;

public class FocusSessionRepository : IFocusSessionRepository
{
    private readonly IDbContextFactory<AppDbContext> _factory;

    public FocusSessionRepository(IDbContextFactory<AppDbContext> factory)
    {
        _factory = factory;
    }

    public async Task AddAsync(FocusSession session, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);

        db.FocusSessions.Add(session);
        await db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<FocusSession>> GetSinceAsync(DateTime fromUtc, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);

        return await db.FocusSessions
            .AsNoTracking()
            .Include(s => s.Task)
            .Where(s => s.StartDateTime >= fromUtc)
            .OrderBy(s => s.StartDateTime)
            .ToListAsync(ct);
    }
}
