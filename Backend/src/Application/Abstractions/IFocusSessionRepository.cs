using FocusLab.Domain.Entities;

namespace FocusLab.Application.Abstractions;

public interface IFocusSessionRepository
{
    Task AddAsync(FocusSession session, CancellationToken ct = default);

    /// <summary>Сесії, що почалися не раніше <paramref name="fromUtc"/>; разом із задачею (Task).</summary>
    Task<IReadOnlyList<FocusSession>> GetSinceAsync(DateTime fromUtc, CancellationToken ct = default);
}
