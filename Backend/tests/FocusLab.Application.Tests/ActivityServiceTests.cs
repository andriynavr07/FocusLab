using FluentAssertions;
using FocusLab.Application.Abstractions;
using FocusLab.Application.Services;
using FocusLab.Domain.Entities;
using NSubstitute;
using Xunit;
using TaskStatus = FocusLab.Domain.Enums.TaskStatus;

namespace FocusLab.Application.Tests;

public class ActivityServiceTests
{
    private static readonly DateTime Now = new(2026, 11, 19, 12, 0, 0, DateTimeKind.Utc);

    private readonly ITaskRepository _tasks = Substitute.For<ITaskRepository>();
    private readonly IFocusSessionRepository _sessions = Substitute.For<IFocusSessionRepository>();
    private readonly IClock _clock = Substitute.For<IClock>();
    private readonly List<TaskItem> _taskList = new();
    private readonly List<FocusSession> _sessionList = new();

    private ActivityService CreateSut(DateTime nowUtc, TimeZoneInfo? tz = null)
    {
        _clock.UtcNow.Returns(nowUtc);
        _tasks.GetAllAsync(Arg.Any<CancellationToken>()).Returns(_taskList);
        _sessions.GetSinceAsync(Arg.Any<DateTime>(), Arg.Any<CancellationToken>()).Returns(_sessionList);
        return new ActivityService(_tasks, _sessions, _clock, tz ?? TimeZoneInfo.Utc);
    }

    private static FocusSession Session(DateTime startUtc, int minutes, bool completed = true) => new()
    {
        TaskId = 1,
        StartDateTime = startUtc,
        DurationMinutes = minutes,
        IsCompleted = completed,
        Task = new TaskItem { Id = 1, Title = "Task" }
    };

    private static TimeZoneInfo Kyiv()
    {
        foreach (var id in new[] { "Europe/Kyiv", "Europe/Kiev", "FLE Standard Time" })
        {
            try { return TimeZoneInfo.FindSystemTimeZoneById(id); }
            catch (TimeZoneNotFoundException) { }
        }
        // Запасний варіант без tzdata: взимку Київ = UTC+2.
        return TimeZoneInfo.CreateCustomTimeZone("Kyiv-test", TimeSpan.FromHours(2), "Kyiv", "Kyiv");
    }

    [Fact]
    public async Task Summary_SumsOnlyTodaySessions()
    {
        _sessionList.Add(Session(Now.AddHours(-3), 25));
        _sessionList.Add(Session(Now.AddHours(-2), 30));
        _sessionList.Add(Session(Now.AddDays(-1), 50));
        _sessionList.Add(Session(Now.AddDays(1), 40));

        var summary = await CreateSut(Now).GetSummaryAsync();

        summary.FocusMinutesToday.Should().Be(55);
        summary.SessionsToday.Should().HaveCount(2);
    }

    [Fact]
    public async Task InterruptedSession_ShownToday_ButNotInStreak()
    {
        _sessionList.Add(Session(Now.AddHours(-1), 10, completed: false));

        var summary = await CreateSut(Now).GetSummaryAsync();

        summary.SessionsToday.Should().HaveCount(1);
        summary.SessionsToday[0].IsCompleted.Should().BeFalse();
        summary.StreakDays.Should().Be(0);
    }

    [Fact]
    public async Task DoneTaskWithoutCompletedAt_Ignored()
    {
        _taskList.Add(new TaskItem { Id = 1, Title = "A", Status = TaskStatus.Done, CompletedAt = null });

        var summary = await CreateSut(Now).GetSummaryAsync();

        summary.TasksCompletedToday.Should().Be(0);
        summary.StreakDays.Should().Be(0);
    }

    [Fact]
    public async Task Summary_UsesLocalDayBoundary()
    {
        // 22:30 UTC 19.11 = 00:30 20.11 за Києвом.
        var sessionUtc = new DateTime(2026, 11, 19, 22, 30, 0, DateTimeKind.Utc);
        var nowUtc = new DateTime(2026, 11, 20, 7, 0, 0, DateTimeKind.Utc);
        _sessionList.Add(Session(sessionUtc, 25));

        var kyiv = await CreateSut(nowUtc, Kyiv()).GetSummaryAsync();
        var utc = await CreateSut(nowUtc, TimeZoneInfo.Utc).GetSummaryAsync();

        kyiv.FocusMinutesToday.Should().Be(25);
        utc.FocusMinutesToday.Should().Be(0);
    }
}
