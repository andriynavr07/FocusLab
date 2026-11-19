using FocusLab.Domain.Entities;

namespace FocusLab.Application.Abstractions;

public interface IAccountRepository
{
    Task<Account?> GetLocalAsync(CancellationToken ct = default);
    Task<Account?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<Account?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task UpdateAsync(Account account, CancellationToken ct = default);
}