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
}
