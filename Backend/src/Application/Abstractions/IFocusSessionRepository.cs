using FocusLab.Domain.Entities;

namespace FocusLab.Application.Abstractions;

public interface IFocusSessionRepository
{
    Task AddAsync(FocusSession session, CancellationToken ct = default);
}
