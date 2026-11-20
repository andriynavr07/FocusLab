using FluentAssertions;
using FocusLab.Application.Abstractions;
using FocusLab.Application.Dto;
using FocusLab.Domain.Enums;
using Frontend.Services;
using Frontend.ViewModels;
using NSubstitute;
using Xunit;
using TaskStatus = FocusLab.Domain.Enums.TaskStatus;

namespace FocusLab.Frontend.Tests;

[Collection(UiPreferencesCollection.Name)]
public class TaskBoardStoreTests
{
    private readonly ITaskService _service = Substitute.For<ITaskService>();
    private readonly IDialogService _dialogs = Substitute.For<IDialogService>();
    private readonly TaskAttachmentStore _attachments = new();
    private readonly FakeClock _clock = new(new DateTime(2026, 11, 19, 14, 30, 0));

    private TaskBoardStore CreateStore() => new(_service, _dialogs, _clock, _attachments);

    private TaskRow AddSavedRow(TaskBoardStore store, int id)
    {
        var row = new TaskRow(_clock) { Id = id, Title = "Task " + id };
        store.Rows.Add(row);
        return row;
    }

    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        for (var i = 0; i < 100 && !condition(); i++)
            await Task.Delay(20);
    }

    [Fact]
    public void Remove_asks_for_confirmation_through_the_dialog_service()
    {
        var store = CreateStore();
        var row = AddSavedRow(store, 5);
        _dialogs.Confirm(Arg.Any<string>(), Arg.Any<string>()).Returns(false);

        store.Remove(row);

        _dialogs.Received(1).Confirm(Arg.Is<string>(m => m.Contains("Task 5")), Arg.Any<string>());
    }

    [Fact]
    public void Declined_removal_keeps_the_row_and_its_attachments()
    {
        var store = CreateStore();
        var row = AddSavedRow(store, 5);
        _attachments.Add(new TaskAttachment { TaskId = 5, Name = "a" });
        _dialogs.Confirm(Arg.Any<string>(), Arg.Any<string>()).Returns(false);

        var removed = store.Remove(row);

        removed.Should().BeFalse();
        store.Rows.Should().Contain(row);
        _attachments.GetFor(5).Should().ContainSingle();
        _service.DidNotReceive().DeleteAsync(Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Confirmed_removal_deletes_the_row_its_attachments_and_the_database_record()
    {
        var store = CreateStore();
        var row = AddSavedRow(store, 5);
        _attachments.Add(new TaskAttachment { TaskId = 5, Name = "a" });
        _dialogs.Confirm(Arg.Any<string>(), Arg.Any<string>()).Returns(true);

        var removed = store.Remove(row);

        removed.Should().BeTrue();
        store.Rows.Should().NotContain(row);
        _attachments.GetFor(5).Should().BeEmpty();
        await WaitUntilAsync(() => _service.ReceivedCalls().Any());
        await _service.Received(1).DeleteAsync(5, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Save_errors_are_reported_through_the_dialog_service()
    {
        var store = CreateStore();
        var row = AddSavedRow(store, 5);
        var error = new InvalidOperationException("db is down");
        _dialogs.Confirm(Arg.Any<string>(), Arg.Any<string>()).Returns(true);
        _service.DeleteAsync(5, Arg.Any<CancellationToken>()).Returns(Task.FromException(error));

        store.Remove(row);

        await WaitUntilAsync(() => _dialogs.ReceivedCalls().Any(c => c.GetMethodInfo().Name == nameof(IDialogService.ShowError)));
        _dialogs.Received(1).ShowError(Arg.Any<string>(), error);
    }

    [Fact]
    public void Store_exposes_the_clock_and_attachments_it_was_created_with()
    {
        var store = CreateStore();

        store.Clock.Should().BeSameAs(_clock);
        store.Attachments.Should().BeSameAs(_attachments);
    }

    // ───── завантаження та збереження ─────

    private static bool CalledUpdate(IEnumerable<NSubstitute.Core.ICall> calls)
        => calls.Any(c => c.GetMethodInfo().Name == nameof(ITaskService.UpdateAsync));

    private static TaskDto Dto(int id, string title = "Task", Priority priority = Priority.Medium,
        TaskStatus status = TaskStatus.Todo, DateTime? deadline = null, DateTime? createdAt = null)
        => new()
        {
            Id = id,
            Title = title,
            Priority = priority,
            Status = status,
            Deadline = deadline,
            CreatedAt = createdAt ?? new DateTime(2026, 11, 10, 12, 0, 0, DateTimeKind.Utc)
        };

    [Fact]
    public async Task Load_DoesNotTriggerUpdate()
    {
        _service.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(new List<TaskDto> { Dto(1, "A", status: TaskStatus.Done), Dto(2, "B", Priority.High) });
        var store = CreateStore();

        await store.LoadAsync();
        await Task.Delay(150);

        store.Rows.Should().HaveCount(2);
        CalledUpdate(_service.ReceivedCalls()).Should().BeFalse();
    }

    [Fact]
    public async Task Add_CallsCreate_AndSetsId()
    {
        _service.CreateAsync(Arg.Any<TaskDto>(), Arg.Any<CancellationToken>()).Returns(Dto(42, "New"));
        var store = CreateStore();
        var row = new TaskRow(_clock) { Title = "New" };

        store.Add(row);
        await WaitUntilAsync(() => row.Id == 42);

        row.Id.Should().Be(42);
        await _service.Received(1).CreateAsync(Arg.Is<TaskDto>(d => d.Title == "New"), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Add_NonTodoStatus_CallsUpdateAfterCreate()
    {
        // Регресія #86: CreateAsync завжди ставить Todo, тому інший статус треба дописати окремим Update.
        _service.CreateAsync(Arg.Any<TaskDto>(), Arg.Any<CancellationToken>()).Returns(Dto(7, "Started"));
        var store = CreateStore();
        var row = new TaskRow(_clock) { Title = "Started", Status = "InProgress" };

        store.Add(row);
        await WaitUntilAsync(() => CalledUpdate(_service.ReceivedCalls()));

        await _service.Received(1).UpdateAsync(
            Arg.Is<TaskDto>(d => d.Id == 7 && d.Status == TaskStatus.InProgress), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EditTitle_CallsUpdateOnce()
    {
        var store = CreateStore();
        var row = AddSavedRow(store, 5);

        row.Title = "Renamed";
        await WaitUntilAsync(() => CalledUpdate(_service.ReceivedCalls()));
        await Task.Delay(100);

        await _service.Received(1).UpdateAsync(
            Arg.Is<TaskDto>(d => d.Id == 5 && d.Title == "Renamed"), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EditBeforeCreateFinished_UpdateRunsAfterCreate()
    {
        var create = new TaskCompletionSource<TaskDto>();
        _service.CreateAsync(Arg.Any<TaskDto>(), Arg.Any<CancellationToken>()).Returns(create.Task);
        var store = CreateStore();
        var row = new TaskRow(_clock) { Title = "Draft" };

        store.Add(row);
        row.Title = "Edited";
        await Task.Delay(100);

        CalledUpdate(_service.ReceivedCalls()).Should().BeFalse("Update має чекати завершення Create");

        create.SetResult(Dto(9, "Draft"));
        await WaitUntilAsync(() => CalledUpdate(_service.ReceivedCalls()));

        Received.InOrder(() =>
        {
            _service.CreateAsync(Arg.Any<TaskDto>(), Arg.Any<CancellationToken>());
            _service.UpdateAsync(Arg.Is<TaskDto>(d => d.Id == 9 && d.Title == "Edited"), Arg.Any<CancellationToken>());
        });
    }

    // ───── мапінг DTO ↔ рядок ─────

    [Fact]
    public async Task Mapping_NullDeadline_DueDateIsCreatedAt()
    {
        var createdAt = new DateTime(2026, 11, 10, 12, 0, 0, DateTimeKind.Utc);
        _service.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(new List<TaskDto> { Dto(1, deadline: null, createdAt: createdAt) });
        var store = CreateStore();

        await store.LoadAsync();

        store.Rows.Single().DueDate.Should().Be(createdAt.ToLocalTime());
    }

    [Theory]
    [InlineData(Priority.Low, "Low")]
    [InlineData(Priority.Medium, "Medium")]
    [InlineData(Priority.High, "High")]
    [InlineData(Priority.Critical, "Critical")]
    public async Task Mapping_Priority_RoundTrip(Priority priority, string key)
    {
        _service.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new List<TaskDto> { Dto(1, priority: priority) });
        _service.CreateAsync(Arg.Any<TaskDto>(), Arg.Any<CancellationToken>()).Returns(Dto(2, priority: priority));
        var store = CreateStore();

        await store.LoadAsync();
        store.Rows.Single().PriorityKey.Should().Be(key);

        store.Add(new TaskRow(_clock) { Title = "Back", PriorityKey = key });
        await WaitUntilAsync(() => _service.ReceivedCalls().Any(c => c.GetMethodInfo().Name == nameof(ITaskService.CreateAsync)));

        await _service.Received(1).CreateAsync(Arg.Is<TaskDto>(d => d.Priority == priority), Arg.Any<CancellationToken>());
    }
}
