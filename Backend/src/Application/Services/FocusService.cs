using FocusLab.Application.Abstractions;
using FocusLab.Domain.Entities;

namespace FocusLab.Application.Services;

public class FocusService : IFocusService
{
    private readonly IFocusSessionRepository _sessions;
    private readonly ITaskRepository _tasks;

    public FocusService(IFocusSessionRepository sessions, ITaskRepository tasks)
    {
        _sessions = sessions;
        _tasks = tasks;
    }

    public async Task SaveSessionAsync(
        int taskId,
        DateTime startUtc,
        int estimatedMinutes,
        int durationMinutes,
        bool isCompleted,
        CancellationToken ct = default)
    {
        if (taskId <= 0) throw new ArgumentOutOfRangeException(nameof(taskId));
        if (estimatedMinutes <= 0) throw new ArgumentOutOfRangeException(nameof(estimatedMinutes));
        if (durationMinutes <= 0) throw new ArgumentOutOfRangeException(nameof(durationMinutes));

        var task = await _tasks.GetByIdAsync(taskId, ct);
        if (task is null)
            throw new KeyNotFoundException($"Задачу з Id={taskId} не знайдено.");

        await _sessions.AddAsync(new FocusSession
        {
            TaskId = taskId,
            StartDateTime = startUtc,
            EstimatedMinutes = estimatedMinutes,
            DurationMinutes = durationMinutes,
            IsCompleted = isCompleted
        }, ct);
    }
}
