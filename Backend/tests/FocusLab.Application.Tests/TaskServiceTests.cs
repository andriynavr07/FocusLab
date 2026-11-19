using FluentAssertions;
using System.Text.Json;
using FocusLab.Application.Abstractions;
using FocusLab.Application.Dto;
using FocusLab.Application.Services;
using FocusLab.Domain.Entities;
using FocusLab.Domain.Enums;
using NSubstitute;
using Xunit;
using TaskStatus = FocusLab.Domain.Enums.TaskStatus;

namespace FocusLab.Application.Tests;

public sealed class TaskServiceTests
{
    private readonly ITaskRepository _repository = Substitute.For<ITaskRepository>();
    private readonly FakeClock _clock = new();

    private TaskService CreateSut() => new(_repository, _clock);

    [Fact]
    public async Task CreateAsync_PersistsTaskFieldsTagsAndChecklist()
    {
        var subtasks = new List<TaskSubtaskDto>
        {
            new("subtask-1", "Підготувати план", false)
        };
        var task = new TaskDto
        {
            Title = "  Нова задача  ",
            Project = "Launch Plan",
            Priority = Priority.High,
            TagIds = [3, 5],
            Subtasks = subtasks
        };
        TaskItem? added = null;
        _repository.AddAsync(Arg.Do<TaskItem>(item => added = item), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var result = await CreateSut().CreateAsync(task);

        await _repository.Received(1).AddAsync(Arg.Any<TaskItem>(), Arg.Any<CancellationToken>());
        added.Should().NotBeNull();
        added!.Title.Should().Be("Нова задача");
        added.Project.Should().Be("Launch Plan");
        added.Priority.Should().Be(Priority.High);
        added.Status.Should().Be(TaskStatus.Todo);
        added.CreatedAt.Should().Be(_clock.UtcNow);
        added.TaskTags.Select(link => link.TagId).Should().BeEquivalentTo(new[] { 3, 5 });
        JsonSerializer.Deserialize<List<TaskSubtaskDto>>(added.ChecklistJson)!
            .Should().ContainSingle().Which.Title.Should().Be("Підготувати план");
        result.TagIds.Should().BeEquivalentTo(new[] { 3, 5 });
        result.Subtasks.Should().BeEquivalentTo(subtasks);
    }

    [Fact]
    public async Task UpdateAsync_UpdatesTagsAndChecklist()
    {
        var entity = new TaskItem
        {
            Id = 12,
            Title = "Стара задача",
            CreatedAt = _clock.UtcNow,
            TaskTags = [new TaskTag { TaskId = 12, TagId = 1 }]
        };
        _repository.GetByIdAsync(12, Arg.Any<CancellationToken>()).Returns(entity);
        var updatedSubtasks = new List<TaskSubtaskDto>
        {
            new("subtask-2", "Перевірити результат", true)
        };

        var result = await CreateSut().UpdateAsync(new TaskDto
        {
            Id = 12,
            Title = "Оновлена задача",
            Status = TaskStatus.InProgress,
            TagIds = [7],
            Subtasks = updatedSubtasks
        });

        await _repository.Received(1).UpdateAsync(entity, Arg.Any<CancellationToken>());
        await _repository.Received(1).SetTagsAsync(12, Arg.Is<IReadOnlyCollection<int>>(ids => ids.SequenceEqual(new[] { 7 })),
            Arg.Any<CancellationToken>());
        entity.Title.Should().Be("Оновлена задача");
        JsonSerializer.Deserialize<List<TaskSubtaskDto>>(entity.ChecklistJson)!
            .Should().ContainSingle().Which.Title.Should().Be("Перевірити результат");
        result.TagIds.Should().ContainSingle().Which.Should().Be(7);
        result.Subtasks.Should().BeEquivalentTo(updatedSubtasks);
    }

    [Fact]
    public async Task CreateAsync_RejectsEmptyTitleWithoutSaving()
    {
        var act = () => CreateSut().CreateAsync(new TaskDto { Title = "  " });

        await act.Should().ThrowAsync<ArgumentException>();
        await _repository.DidNotReceiveWithAnyArgs().AddAsync(default!);
    }
}
