using FluentAssertions;
using FocusLab.Application.Abstractions;
using FocusLab.Application.Dto;
using FocusLab.Application.Services;
using FocusLab.Domain.Entities;
using FocusLab.Domain.Enums;
using NSubstitute;
using Xunit;
using TaskStatus = FocusLab.Domain.Enums.TaskStatus;

namespace FocusLab.Application.Tests;

public class TaskServiceTests
{
    private static readonly DateTime Now = new(2026, 11, 19, 10, 0, 0, DateTimeKind.Utc);

    private readonly ITaskRepository _repo = Substitute.For<ITaskRepository>();
    private readonly IClock _clock = Substitute.For<IClock>();
    private readonly TaskService _sut;
    private TaskItem? _saved;

    public TaskServiceTests()
    {
        _clock.UtcNow.Returns(Now);
        _repo.AddAsync(Arg.Do<TaskItem>(t => _saved = t), Arg.Any<CancellationToken>());
        _sut = new TaskService(_repo, _clock);
    }

    private static TaskDto ValidDto() => new() { Title = "Task", Priority = Priority.High };

    private TaskItem ExistingTask(TaskStatus status, DateTime? completedAt = null)
    {
        var entity = new TaskItem { Id = 1, Title = "T", Status = status, CompletedAt = completedAt };
        _repo.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(entity);
        return entity;
    }

    [Fact]
    public async Task CreateAsync_IgnoresDtoStatus_SetsTodoAndClockTime()
    {
        var dto = ValidDto();
        dto.Status = TaskStatus.Done;
        dto.CreatedAt = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        var result = await _sut.CreateAsync(dto);

        _saved!.Status.Should().Be(TaskStatus.Todo);
        _saved.CreatedAt.Should().Be(Now);
        result.Status.Should().Be(TaskStatus.Todo);
        result.CreatedAt.Should().Be(Now);
    }

    [Fact]
    public async Task CreateAsync_TrimsTitle()
    {
        var dto = ValidDto();
        dto.Title = "   Hello   ";

        await _sut.CreateAsync(dto);

        _saved!.Title.Should().Be("Hello");
    }

    [Fact]
    public async Task CreateAsync_TitleOver255_Throws()
    {
        var dto = ValidDto();
        dto.Title = new string('a', 256);

        var act = () => _sut.CreateAsync(dto);

        await act.Should().ThrowAsync<ArgumentException>();
        await _repo.DidNotReceive().AddAsync(Arg.Any<TaskItem>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_ProjectOver100_Throws()
    {
        var dto = ValidDto();
        dto.Project = new string('p', 101);

        var act = () => _sut.CreateAsync(dto);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public async Task CreateAsync_NonPositiveEstimate_StoredAsNull(int estimate)
    {
        var dto = ValidDto();
        dto.EstimatedMinutes = estimate;

        await _sut.CreateAsync(dto);

        _saved!.EstimatedMinutes.Should().BeNull();
    }

    [Fact]
    public async Task CreateAsync_DuplicateTagIds_AreDistinct()
    {
        var dto = ValidDto();
        dto.TagIds = new List<int> { 1, 2, 2, 1, 3 };

        await _sut.CreateAsync(dto);

        _saved!.TaskTags.Select(t => t.TagId).Should().BeEquivalentTo(new[] { 1, 2, 3 });
    }

    [Fact]
    public async Task UpdateAsync_UnknownId_ThrowsKeyNotFound()
    {
        _repo.GetByIdAsync(42, Arg.Any<CancellationToken>()).Returns((TaskItem?)null);
        var dto = ValidDto();
        dto.Id = 42;

        var act = () => _sut.UpdateAsync(dto);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task UpdateAsync_ToDone_SetsCompletedAt()
    {
        var entity = ExistingTask(TaskStatus.InProgress);
        var dto = ValidDto();
        dto.Id = 1;
        dto.Status = TaskStatus.Done;

        var result = await _sut.UpdateAsync(dto);

        entity.CompletedAt.Should().Be(Now);
        result.CompletedAt.Should().Be(Now);
    }

    [Fact]
    public async Task UpdateAsync_DoneToTodo_ClearsCompletedAt()
    {
        var entity = ExistingTask(TaskStatus.Done, Now.AddDays(-1));
        var dto = ValidDto();
        dto.Id = 1;
        dto.Status = TaskStatus.Todo;

        await _sut.UpdateAsync(dto);

        entity.Status.Should().Be(TaskStatus.Todo);
        entity.CompletedAt.Should().BeNull();
    }

    [Fact]
    public async Task UpdateAsync_SameStatus_KeepsCompletedAt()
    {
        var completed = Now.AddDays(-3);
        var entity = ExistingTask(TaskStatus.Done, completed);
        var dto = ValidDto();
        dto.Id = 1;
        dto.Status = TaskStatus.Done;

        await _sut.UpdateAsync(dto);

        entity.CompletedAt.Should().Be(completed);
    }

    [Fact]
    public async Task GetAll_ChecklistJsonNull_ThrowsInvalidOperation()
    {
        // JSON-літерал "null" десеріалізується в null → InvalidOperationException.
        var broken = new TaskItem { Id = 1, Title = "T", ChecklistJson = "null" };
        _repo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new List<TaskItem> { broken });

        var act = () => _sut.GetAllAsync();

        await act.Should().ThrowAsync<InvalidOperationException>();
    }
}