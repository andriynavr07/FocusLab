namespace FocusLab.Application.Abstractions;

/// <summary>Запис фокус-сесій у БД (FL-B16).</summary>
public interface IFocusService
{
    /// <param name="estimatedMinutes">Запланована тривалість інтервалу.</param>
    /// <param name="durationMinutes">Фактична тривалість (для завершеної сесії збігається із запланованою).</param>
    /// <exception cref="KeyNotFoundException">Задачу не знайдено (наприклад, видалено під час сесії).</exception>
    Task SaveSessionAsync(
        int taskId,
        DateTime startUtc,
        int estimatedMinutes,
        int durationMinutes,
        bool isCompleted,
        CancellationToken ct = default);
}
