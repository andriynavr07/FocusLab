using FocusLab.Application.Abstractions;
using FocusLab.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FocusLab.Infrastructure.Persistence;

public class AccountRepository : IAccountRepository
{
    private readonly IDbContextFactory<AppDbContext> _factory;

    public AccountRepository(IDbContextFactory<AppDbContext> factory) => _factory = factory;

    public async Task<Account?> GetLocalAsync(CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        return await db.Accounts.AsNoTracking().FirstOrDefaultAsync(a => a.Id == Account.LocalAccountId, ct);
    }

    public async Task<Account?> GetByEmailAsync(string email, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        return await db.Accounts.AsNoTracking().FirstOrDefaultAsync(a => a.Email == email, ct);
    }

    public async Task UpdateAsync(Account account, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        db.Accounts.Update(account);
        await db.SaveChangesAsync(ct);
    }
}