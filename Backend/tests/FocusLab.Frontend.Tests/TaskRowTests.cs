using FluentAssertions;
using Frontend.Services;
using Frontend.ViewModels;
using Xunit;

namespace FocusLab.Frontend.Tests;

[Collection(UiPreferencesCollection.Name)]
public class TaskRowTests
{
    private static readonly DateTime Now = new(2026, 11, 19, 14, 30, 0);

    [Fact]
    public void New_row_is_due_today_according_to_the_clock()
    {
        var row = new TaskRow(new FakeClock(Now));

        row.DueDate.Should().Be(Now.Date);
        row.DueText.Should().Be(UiPreferences.Instance.Translate("Сьогодні"));
    }

    [Fact]
    public void IsOverdue_PastDueTask_True()
    {
        var row = new TaskRow(new FakeClock(Now)) { DueDate = Now.Date.AddDays(-1) };

        row.IsOverdue.Should().BeTrue();
    }

    [Fact]
    public void IsOverdue_DoneTask_False()
    {
        var row = new TaskRow(new FakeClock(Now)) { DueDate = Now.Date.AddDays(-1) };

        row.IsDone = true;

        row.IsOverdue.Should().BeFalse();
    }

    [Fact]
    public void Row_due_today_is_not_overdue()
    {
        var row = new TaskRow(new FakeClock(Now)) { DueDate = Now.Date };

        row.IsOverdue.Should().BeFalse();
    }

    [Fact]
    public void Overdue_depends_on_the_clock_not_on_the_system_time()
    {
        var clock = new FakeClock(Now);
        var row = new TaskRow(clock) { DueDate = Now.Date };

        clock.Now = Now.AddDays(1);

        row.IsOverdue.Should().BeTrue();
    }

    [Fact]
    public void Status_Done_SetsCompletedAt()
    {
        var row = new TaskRow(new FakeClock(Now));

        row.Status = "Done";

        row.CompletedAt.Should().Be(Now);
    }

    [Fact]
    public void Status_BackToTodo_ClearsCompletedAt()
    {
        var row = new TaskRow(new FakeClock(Now)) { Status = "Done" };

        row.Status = "Todo";

        row.CompletedAt.Should().BeNull();
    }
}
