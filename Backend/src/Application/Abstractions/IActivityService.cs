using FocusLab.Application.Dto;

namespace FocusLab.Application.Abstractions;

/// <summary>Статистика активності користувача, що обчислюється з даних БД (задачі та фокус-сесії).</summary>
public interface IActivityService
{
    Task<ActivitySummary> GetSummaryAsync(CancellationToken ct = default);
}
