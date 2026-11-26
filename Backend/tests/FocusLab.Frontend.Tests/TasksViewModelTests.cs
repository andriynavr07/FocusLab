using FluentAssertions;
using FocusLab.Application.Abstractions;
using FocusLab.Application.Dto;
using Frontend.Services;
using Frontend.ViewModels;
using NSubstitute;
using Xunit;

namespace FocusLab.Frontend.Tests;

public class TasksViewModelTests
{
    private readonly ITaskService _service = Substitute.For<ITaskService>();
    private readonly FakeClock _clock = new(new DateTime(2026, 11, 19, 14, 30, 0));
    private readonly TaskBoardStore _store;
    private readonly TasksViewModel _viewModel;

    public TasksViewModelTests()
    {
        var catalog = Substitute.For<ICatalogService>();
        catalog.GetTagsAsync().Returns(new List<TagDto>());
        _store = new TaskBoardStore(_service, Substitute.For<IDialogService>(), _clock, new TaskAttachmentStore(), TestLocalizers.Ukrainian);
        _viewModel = new TasksViewModel(_store, catalog);
    }

    private TaskRow AddRow(string title, string status = "Todo", int id = 0)
    {
        var row = new TaskRow(_clock, TestLocalizers.Ukrainian) { Id = id, Title = title, Status = status, DueDate = _clock.Today.AddHours(18) };
        _store.Rows.Add(row);
        return row;
    }

    // ───── підзадачі ─────

    [Theory]
    [InlineData("", false)]
    [InlineData("   ", false)]
    [InlineData("Buy milk", true)]
    public void CanAddSubtask_EmptyOrBlank_False(string title, bool expected)
    {
        _viewModel.NewSubtaskTitle = title;

        _viewModel.CanAddSubtask.Should().Be(expected);
    }

    [Theory]
    [InlineData(255, true)]
    [InlineData(256, false)]
    public void CanAddSubtask_EmptyOrOver255_False(int length, bool expected)
    {
        _viewModel.NewSubtaskTitle = new string('a', length);

        _viewModel.CanAddSubtask.Should().Be(expected);
    }

    // ───── список, фільтри ─────

    [Fact]
    public void Filter_ByStatus_ShowsOnlyMatching()
    {
        AddRow("Todo task", "Todo");
        var done = AddRow("Done task", "Done");
        AddRow("In progress", "InProgress");

        _viewModel.SelectedStatus = _viewModel.StatusFilters[5]; // «Виконано»

        _viewModel.VisibleRows.Should().Equal(done);
    }

    [Fact]
    public void Filter_AllStatuses_ShowsEveryRow()
    {
        AddRow("A", "Todo");
        AddRow("B", "Done");

        _viewModel.SelectedStatus = _viewModel.StatusFilters[5];
        _viewModel.SelectedStatus = _viewModel.StatusFilters[0];

        _viewModel.VisibleRows.Should().HaveCount(2);
    }

    [Fact]
    public void Filter_NullSelection_IsIgnored()
    {
        AddRow("A");

        _viewModel.SelectedStatus = null!;

        _viewModel.VisibleRows.Should().HaveCount(1);
    }

    [Fact]
    public void NewRow_AppearsInVisibleRowsImmediately()
    {
        var row = AddRow("Fresh");

        _viewModel.VisibleRows.Should().Contain(row);
    }

    [Fact]
    public void AllDone_EmptyList_False()
    {
        _viewModel.AllDone.Should().BeFalse();
    }

    [Fact]
    public void AllDone_EveryVisibleRowDone_True()
    {
        AddRow("A", "Done");
        AddRow("B", "Done");

        _viewModel.AllDone.Should().BeTrue();
    }

    // ───── редагування ─────

    [Fact]
    public void FormTitle_InEditMode_IsEdit()
    {
        var row = AddRow("Task", id: 3);
        _viewModel.FormTitle.Should().Be("Нове завдання");

        _viewModel.OpenEditTask(row);

        _viewModel.IsEditing.Should().BeTrue();
        _viewModel.FormTitle.Should().Be("Редагувати завдання");
    }

    [Fact]
    public void OpenEdit_FillsFormFromRow()
    {
        var row = AddRow("Write report", id: 3);
        row.Description = "Quarterly numbers";
        row.PriorityKey = "High";
        row.DueDate = new DateTime(2026, 11, 25, 14, 35, 0);
        row.EstimatedMinutes = 45;

        _viewModel.OpenEditTask(row);

        _viewModel.IsAddOpen.Should().BeTrue();
        _viewModel.NewTitle.Should().Be("Write report");
        _viewModel.NewDescription.Should().Be("Quarterly numbers");
        _viewModel.NewPriority.Key.Should().Be("High");
        _viewModel.NewDueDate.Should().Be(new DateTime(2026, 11, 25));
        _viewModel.NewDueHour.Should().Be(14);
        _viewModel.NewDueMinute.Should().Be(35);
        _viewModel.NewEstimate.Minutes.Should().Be(45);
    }

    [Fact]
    public void SubmitEdit_UpdatesRow_NotAddsNew()
    {
        var row = AddRow("Old title", id: 3);
        var rowsBefore = _store.Rows.Count;
        _viewModel.OpenEditTask(row);
        _viewModel.NewTitle = "New title";

        _viewModel.CreateTaskCommand.Execute(null);

        row.Title.Should().Be("New title");
        _store.Rows.Should().HaveCount(rowsBefore);
        _viewModel.IsAddOpen.Should().BeFalse();
        _service.DidNotReceive().CreateAsync(Arg.Any<TaskDto>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public void Submit_InCreateMode_AddsNewRow()
    {
        _viewModel.OpenNewTaskCommand.Execute(null);
        _viewModel.NewTitle = "Brand new";

        _viewModel.CreateTaskCommand.Execute(null);

        _store.Rows.Should().ContainSingle(r => r.Title == "Brand new");
        _viewModel.VisibleRows.Should().ContainSingle(r => r.Title == "Brand new");
    }
}
