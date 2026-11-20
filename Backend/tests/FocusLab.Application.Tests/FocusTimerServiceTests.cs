using FluentAssertions;
using FocusLab.Application.Abstractions;
using FocusLab.Application.Focus;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace FocusLab.Application.Tests;

public class FocusTimerServiceTests
{
    private readonly IClock _clock = Substitute.For<IClock>();
    private readonly IFocusSettingsProvider _settings = Substitute.For<IFocusSettingsProvider>();
    private DateTime _now = new(2026, 11, 19, 10, 0, 0, DateTimeKind.Utc);

    private FocusTimerService CreateSut()
    {
        _clock.UtcNow.Returns(_ => _now);
        _settings.Current.Returns(new FocusSettings
        {
            FocusMinutes = 25,
            ShortBreakMinutes = 5,
            LongBreakMinutes = 15,
            SessionsBeforeLongBreak = 4,
            AutoStartNext = false
        });

        return new FocusTimerService(
            _clock,
            Substitute.For<IFocusService>(),
            Substitute.For<INotificationService>(),
            Substitute.For<ISoundPlayer>(),
            _settings,
            NullLogger<FocusTimerService>.Instance,
            Timeout.InfiniteTimeSpan);
    }

    private void Advance(TimeSpan span) => _now += span;

    [Fact]
    public void Resume_AfterPause_ContinuesFromRemaining()
    {
        using var sut = CreateSut();
        sut.Start();
        Advance(TimeSpan.FromMinutes(10));
        sut.Pause();
        var remainingAtPause = sut.Remaining;

        Advance(TimeSpan.FromMinutes(30));
        sut.Resume();
        Advance(TimeSpan.FromMinutes(5));

        remainingAtPause.Should().Be(TimeSpan.FromMinutes(15));
        sut.Remaining.Should().Be(TimeSpan.FromMinutes(10));
        sut.Status.Should().Be(TimerStatus.Running);
        sut.Phase.Should().Be(TimerPhase.Focus);
    }

    [Fact]
    public async Task SkipBreak_DuringBreak_SwitchesToFocus()
    {
        using var sut = CreateSut();
        sut.Start();
        Advance(TimeSpan.FromMinutes(25));
        await sut.TickAsync();
        sut.Phase.Should().Be(TimerPhase.ShortBreak);

        sut.SkipBreak();

        sut.Phase.Should().Be(TimerPhase.Focus);
        sut.Status.Should().Be(TimerStatus.Idle);
        sut.Remaining.Should().Be(TimeSpan.FromMinutes(25));
    }
}