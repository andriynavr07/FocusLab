using FluentAssertions;
using FocusLab.Application.Services;
using Xunit;

namespace FocusLab.Application.Tests;

public sealed class StreakCalculatorTests
{
    private static readonly DateTime Today = new(2026, 11, 6);

    [Fact]
    public void NoActivity_ReturnsZero()
        => StreakCalculator.Calculate([], Today).Should().Be(0);

    [Fact]
    public void ConsecutiveDaysIncludingToday_AreCounted()
    {
        var days = new[] { Today, Today.AddDays(-1), Today.AddDays(-2) };
        StreakCalculator.Calculate(days, Today).Should().Be(3);
    }

    [Fact]
    public void NoActivityToday_StreakEndsYesterday()
    {
        var days = new[] { Today.AddDays(-1), Today.AddDays(-2) };
        StreakCalculator.Calculate(days, Today).Should().Be(2);
    }

    [Fact]
    public void GapBreaksStreak()
    {
        var days = new[] { Today, Today.AddDays(-1), Today.AddDays(-3), Today.AddDays(-4) };
        StreakCalculator.Calculate(days, Today).Should().Be(2);
    }

    [Fact]
    public void LastActivityTwoDaysAgo_ResetsToZero()
        => StreakCalculator.Calculate([Today.AddDays(-2)], Today).Should().Be(0);

    [Fact]
    public void SeveralEventsInOneDay_CountOnce()
    {
        var days = new[] { Today.AddHours(9), Today.AddHours(15), Today.AddDays(-1).AddHours(10) };
        StreakCalculator.Calculate(days, Today).Should().Be(2);
    }
}
