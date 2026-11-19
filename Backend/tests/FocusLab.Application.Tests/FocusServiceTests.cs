using FluentAssertions;
using FocusLab.Application.Abstractions;
using FocusLab.Application.Services;
using FocusLab.Domain.Entities;
using NSubstitute;
using Xunit;

namespace FocusLab.Application.Tests;

public sealed class FocusServiceTests
{
    private readonly IFocusSessionRepository _sessions = Substitute.For<IFocusSessionRepository>();
    private readonly ITaskRepository _tasks = Substitute.For<ITaskRepository>();

    private FocusService CreateSut() => new(_sessions, _tasks);

    [Fact]
    public async Task SaveSessionAsync_StoresSessionWithAllFields()
    {
        var start = new DateTime(2026, 11, 4, 9, 0, 0, DateTimeKind.Utc);
        _tasks.GetByIdAsync(5, Arg.Any<CancellationToken>()).Returns(new TaskItem { Id = 5 });

        await CreateSut().SaveSessionAsync(5, start, 25, 10, false);

        await _sessions.Received(1).AddAsync(
            Arg.Is<FocusSession>(s =>
                s.TaskId == 5 && s.StartDateTime == start &&
                s.EstimatedMinutes == 25 && s.DurationMinutes == 10 && !s.IsCompleted),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SaveSessionAsync_UnknownTask_ThrowsKeyNotFound()
    {
        _tasks.GetByIdAsync(5, Arg.Any<CancellationToken>()).Returns((TaskItem?)null);

        var act = () => CreateSut().SaveSessionAsync(5, DateTime.UtcNow, 25, 25, true);

        await act.Should().ThrowAsync<KeyNotFoundException>();
        await _sessions.DidNotReceiveWithAnyArgs().AddAsync(default!);
    }

    [Theory]
    [InlineData(0, 25, 25)]
    [InlineData(1, 0, 25)]
    [InlineData(1, 25, 0)]
    public async Task SaveSessionAsync_InvalidArguments_Throw(int taskId, int estimated, int duration)
    {
        var act = () => CreateSut().SaveSessionAsync(taskId, DateTime.UtcNow, estimated, duration, true);

        await act.Should().ThrowAsync<ArgumentOutOfRangeException>();
    }
}
