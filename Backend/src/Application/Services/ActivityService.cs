using FocusLab.Application.Abstractions;
using FocusLab.Application.Dto;
using TaskStatus = FocusLab.Domain.Enums.TaskStatus;

namespace FocusLab.Application.Services;

public class ActivityService : IActivityService
{
    /// <summary>Скільки днів історії аналізуємо для серії.</summary>
    private const int HistoryDays = 400;

    private readonly ITaskRepository _tasks;
    private readonly IFocusSessionRepository _sessions;
    private readonly IClock _clock;
    private readonly TimeZoneInfo _timeZone;

    public ActivityService(
        ITaskRepository tasks,
        IFocusSessionRepository sessions,
        IClock clock,
        TimeZoneInfo? timeZone = null)
    {
        _tasks = tasks;
        _sessions = sessions;
        _clock = clock;
        _timeZone = timeZone ?? TimeZoneInfo.Local;
    }

    public async Task<ActivitySummary> GetSummaryAsync(CancellationToken ct = default)
    {
        var nowUtc = _clock.UtcNow;
        var today = ToLocal(nowUtc).Date;

        var sessions = await _sessions.GetSinceAsync(nowUtc.AddDays(-HistoryDays), ct);
        var tasks = await _tasks.GetAllAsync(ct);

        var activeDays = new List<DateTime>();
        activeDays.AddRange(sessions.Where(s => s.IsCompleted).Select(s => ToLocal(s.StartDateTime).Date));
        activeDays.AddRange(tasks
            .Where(t => t.Status == TaskStatus.Done && t.CompletedAt.HasValue)
            .Select(t => ToLocal(t.CompletedAt!.Value).Date));

        var todaySessions = sessions
            .Where(s => ToLocal(s.StartDateTime).Date == today)
            .OrderBy(s => s.StartDateTime)
            .Select(s => new FocusSessionDto(
                s.TaskId,
                s.Task?.Title ?? string.Empty,
                s.StartDateTime,
                s.DurationMinutes,
                s.IsCompleted))
            .ToList();

        var tasksDoneToday = tasks.Count(t =>
            t.Status == TaskStatus.Done
            && t.CompletedAt.HasValue
            && ToLocal(t.CompletedAt.Value).Date == today);

        return new ActivitySummary(
            StreakCalculator.Calculate(activeDays, today),
            todaySessions.Sum(s => s.DurationMinutes),
            tasksDoneToday,
            todaySessions);
    }

    private DateTime ToLocal(DateTime utc)
        => TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), _timeZone);
}
