using FluentAssertions;
using FocusLab.Application.Abstractions;
using Frontend.Services;
using Frontend.ViewModels;
using NSubstitute;
using Xunit;

namespace FocusLab.Frontend.Tests;

[Collection(UiPreferencesCollection.Name)]
public class CalendarViewModelTests
{
    private static readonly DateTime Now = new(2026, 11, 19, 14, 30, 0);

    [Fact]
    public void First_event_of_the_day_starts_at_nine()
    {
        var ev = CalendarViewModel.CreateEvent(new TaskRow(new FakeClock(Now)) { Title = "A" }, 0);

        ev.TimeRange.Should().Be("09:00 - 09:25");
    }

    [Fact]
    public void Fifteenth_event_still_starts_at_23_00()
    {
        var ev = CalendarViewModel.CreateEvent(new TaskRow(new FakeClock(Now)) { Title = "A" }, 14);

        ev.TimeRange.Should().Be("23:00 - 23:25");
    }

    [Theory]
    [InlineData(15)]
    [InlineData(16)]
    [InlineData(40)]
    public void Events_beyond_the_last_slot_never_leave_the_day(int index)
    {
        var ev = CalendarViewModel.CreateEvent(new TaskRow(new FakeClock(Now)) { Title = "A" }, index);

        ev.Start.Should().BeGreaterOrEqualTo(TimeSpan.FromHours(CalendarViewModel.FirstEventHour));
        ev.Start.Should().BeLessThan(TimeSpan.FromHours(24));
        ev.TimeRange.Should().NotBe("00:00 - 00:25");
    }

    [Fact]
    public void Every_event_of_a_busy_day_has_a_start_time_within_the_day()
    {
        var row = new TaskRow(new FakeClock(Now)) { Title = "A" };

        var starts = Enumerable.Range(0, 60)
            .Select(i => CalendarViewModel.CreateEvent(row, i).Start)
            .ToList();

        starts.Should().OnlyContain(s => s >= TimeSpan.FromHours(9) && s < TimeSpan.FromHours(24));
    }

    [Fact]
    public void Today_cell_is_taken_from_the_clock()
    {
        var store = new TaskBoardStore(Substitute.For<ITaskService>(), Substitute.For<IDialogService>(), new FakeClock(Now));
        var calendar = new CalendarViewModel(store);

        var today = calendar.Days.Where(d => d.IsToday).ToList();

        today.Should().ContainSingle();
        today[0].Date.Should().Be(Now.Date);
        today[0].IsSelected.Should().BeTrue();
    }

    // ───── сітка місяця ─────

    private TaskBoardStore CreateStore(DateTime now)
        => new(Substitute.For<ITaskService>(), Substitute.For<IDialogService>(), new FakeClock(now));

    private static TaskRow RowOn(DateTime due, string title = "Task", bool done = false)
        => new(new FakeClock(due)) { Title = title, DueDate = due, Status = done ? "Done" : "Todo" };

    [Theory]
    [InlineData(2026, 11, 6, 42)] // 1 листопада 2026 — неділя
    [InlineData(2027, 2, 0, 28)]  // 1 лютого 2027 — понеділок
    public void Days_StartOnMonday_CountMultipleOf7(int year, int month, int leadingBlanks, int totalCells)
    {
        var now = new DateTime(year, month, 10, 12, 0, 0);
        var calendar = new CalendarViewModel(CreateStore(now));

        calendar.Days.Count.Should().Be(totalCells);
        (calendar.Days.Count % 7).Should().Be(0);
        calendar.Days.TakeWhile(d => !d.HasDate).Count().Should().Be(leadingBlanks);
        calendar.Days.First(d => d.HasDate).Date.Should().Be(new DateTime(year, month, 1));
    }

    [Fact]
    public void DonePercent_CountsOnlyCurrentMonth()
    {
        var store = CreateStore(Now);
        store.Rows.Add(RowOn(new DateTime(2026, 11, 3, 10, 0, 0), done: true));
        store.Rows.Add(RowOn(new DateTime(2026, 11, 20, 10, 0, 0), done: false));
        store.Rows.Add(RowOn(new DateTime(2026, 12, 5, 10, 0, 0), done: true));
        var calendar = new CalendarViewModel(store);

        calendar.DonePercentText.Should().Be("50%");
    }

    [Fact]
    public void DonePercent_NoTasks_Zero()
    {
        var calendar = new CalendarViewModel(CreateStore(Now));

        calendar.DonePercentText.Should().Be("0%");
    }

    [Fact]
    public void Search_FiltersSelectedEvents()
    {
        var store = CreateStore(Now);
        store.Rows.Add(RowOn(Now.Date.AddHours(10), "Buy milk"));
        store.Rows.Add(RowOn(Now.Date.AddHours(11), "Write report"));
        var calendar = new CalendarViewModel(store);
        calendar.SelectedEvents.Should().HaveCount(2);

        calendar.SearchText = "milk";

        calendar.SelectedEvents.Should().ContainSingle(e => e.Title == "Buy milk");
    }

    [Theory]
    [InlineData(15)]
    [InlineData(16)]
    [InlineData(30)]
    public void FifteenTasksOneDay_TimeRangeDoesNotWrap(int taskCount)
    {
        var store = CreateStore(Now);
        for (var i = 0; i < taskCount; i++)
            store.Rows.Add(RowOn(Now.Date.AddHours(10), $"Task {i:00}"));
        var calendar = new CalendarViewModel(store);

        calendar.SelectedEvents.Should().HaveCount(taskCount);
        calendar.SelectedEvents.Select(e => e.TimeRange).Should().NotContain("00:00 - 00:25");
        calendar.SelectedEvents.Should().OnlyContain(e =>
            e.Start >= TimeSpan.FromHours(9) && e.Start < TimeSpan.FromHours(24));
    }
}
