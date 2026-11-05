using FluentAssertions;
using FocusLab.Application.Abstractions;
using FocusLab.Application.Focus;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace FocusLab.Application.Tests;

public sealed class FocusTimerServiceTests
{
    private readonly FakeClock _clock = new();
    private readonly IFocusService _focus = Substitute.For<IFocusService>();
    private readonly INotificationService _notifications = Substitute.For<INotificationService>();
    private readonly ISoundPlayer _sound = Substitute.For<ISoundPlayer>();
    private readonly IFocusSettingsProvider _settings = Substitute.For<IFocusSettingsProvider>();

    public FocusTimerServiceTests()
    {
        _focus.SaveSessionAsync(default, default, default, default, default, default)
              .ReturnsForAnyArgs(Task.CompletedTask);
    }

    /// <summary>Сторож вимкнено (InfiniteTimeSpan): тік викликається вручну, тести детерміновані.</summary>
    private FocusTimerService CreateSut(FocusSettings? settings = null)
    {
        _settings.Current.Returns(settings ?? new FocusSettings { AutoStartNext = false });
        return new FocusTimerService(_clock, _focus, _notifications, _sound, _settings,
            NullLogger<FocusTimerService>.Instance, Timeout.InfiniteTimeSpan);
    }

    [Fact]
    public void Pause_FreezesRemainingTime()
    {
        using var sut = CreateSut();
        sut.Start(1);

        _clock.Advance(TimeSpan.FromMinutes(10));
        sut.Pause();
        _clock.Advance(TimeSpan.FromMinutes(30));            // час на паузі не рахується

        sut.Remaining.Should().Be(TimeSpan.FromMinutes(15));

        sut.Resume();
        _clock.Advance(TimeSpan.FromMinutes(5));

        sut.Remaining.Should().Be(TimeSpan.FromMinutes(10));
    }

    [Fact]
    public async Task Elapsed_IsBasedOnClock_NotOnTickCount()
    {
        using var sut = CreateSut();
        sut.Start(1);

        _clock.Advance(TimeSpan.FromMinutes(25));            // «сон ПК»: жодного тіка не було
        await sut.TickAsync();

        sut.Phase.Should().Be(TimerPhase.ShortBreak);
    }

    [Fact]
    public async Task FocusCompleted_SavesSession_NotifiesAndSwitchesToShortBreak()
    {
        using var sut = CreateSut();
        var start = _clock.UtcNow;
        sut.Start(42);

        _clock.Advance(TimeSpan.FromMinutes(25));
        await sut.TickAsync();

        await _focus.Received(1).SaveSessionAsync(42, start, 25, 25, true, Arg.Any<CancellationToken>());
        _sound.Received(1).PlayIntervalCompleted();
        _notifications.Received(1).NotifyIntervalCompleted(TimerPhase.Focus, TimerPhase.ShortBreak);
        sut.Phase.Should().Be(TimerPhase.ShortBreak);
        sut.Remaining.Should().Be(TimeSpan.FromMinutes(5));
    }

    [Fact]
    public async Task SecondTick_DoesNotSaveSessionTwice()
    {
        using var sut = CreateSut();
        sut.Start(1);
        _clock.Advance(TimeSpan.FromMinutes(25));

        await sut.TickAsync();
        await sut.TickAsync();

        await _focus.Received(1).SaveSessionAsync(1, Arg.Any<DateTime>(), 25, 25, true, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task FourthSession_LeadsToLongBreak_ThenCycleResets()
    {
        using var sut = CreateSut();

        for (var i = 1; i <= 4; i++)
        {
            sut.Start(1);
            _clock.Advance(TimeSpan.FromMinutes(25));
            await sut.TickAsync();

            if (i < 4)
            {
                sut.Phase.Should().Be(TimerPhase.ShortBreak);
                sut.SkipBreak();
            }
        }

        sut.Phase.Should().Be(TimerPhase.LongBreak);
        sut.Remaining.Should().Be(TimeSpan.FromMinutes(15));

        sut.SkipBreak();

        sut.Phase.Should().Be(TimerPhase.Focus);
        sut.CompletedSessionsInCycle.Should().Be(0);
    }

    [Fact]
    public async Task AutoStartNext_StartsBreakAutomatically()
    {
        using var sut = CreateSut(new FocusSettings { AutoStartNext = true });
        sut.Start(1);

        _clock.Advance(TimeSpan.FromMinutes(25));
        await sut.TickAsync();

        sut.Phase.Should().Be(TimerPhase.ShortBreak);
        sut.Status.Should().Be(TimerStatus.Running);
    }

    [Fact]
    public async Task Stop_AfterTenMinutes_SavesInterruptedSession()
    {
        using var sut = CreateSut();
        var start = _clock.UtcNow;
        sut.Start(7);

        _clock.Advance(TimeSpan.FromMinutes(10));
        await sut.StopAsync();

        await _focus.Received(1).SaveSessionAsync(7, start, 25, 10, false, Arg.Any<CancellationToken>());
        sut.Status.Should().Be(TimerStatus.Idle);
        sut.Phase.Should().Be(TimerPhase.Focus);
    }

    [Fact]
    public async Task Stop_UnderOneMinute_SavesNothing()
    {
        using var sut = CreateSut();
        sut.Start(7);

        _clock.Advance(TimeSpan.FromSeconds(30));
        await sut.StopAsync();

        await _focus.DidNotReceiveWithAnyArgs()
            .SaveSessionAsync(default, default, default, default, default, default);
    }

    [Fact]
    public async Task Stop_WhenSavingInterruptedDisabled_SavesNothing()
    {
        using var sut = CreateSut(new FocusSettings { AutoStartNext = false, SaveInterruptedSessions = false });
        sut.Start(7);

        _clock.Advance(TimeSpan.FromMinutes(10));
        await sut.StopAsync();

        await _focus.DidNotReceiveWithAnyArgs()
            .SaveSessionAsync(default, default, default, default, default, default);
    }

    [Fact]
    public async Task SaveFailure_KeepsSessionPending_AndRetrySucceeds()
    {
        _focus.SaveSessionAsync(default, default, default, default, default, default)
              .ReturnsForAnyArgs(Task.FromException(new InvalidOperationException("db locked")),
                                 Task.CompletedTask);
        using var sut = CreateSut();
        TimerErrorEventArgs? error = null;
        sut.ErrorOccurred += (_, e) => error = e;

        sut.Start(1);
        _clock.Advance(TimeSpan.FromMinutes(25));
        await sut.TickAsync();

        sut.PendingUnsavedSessions.Should().Be(1);
        error.Should().NotBeNull();

        await sut.RetryPendingAsync();

        sut.PendingUnsavedSessions.Should().Be(0);
    }

    [Fact]
    public async Task SaveFailure_WhenTaskWasDeleted_DropsSessionWithoutRetry()
    {
        _focus.SaveSessionAsync(default, default, default, default, default, default)
              .ReturnsForAnyArgs(Task.FromException(new KeyNotFoundException("task deleted")));
        using var sut = CreateSut();

        sut.Start(1);
        _clock.Advance(TimeSpan.FromMinutes(25));
        await sut.TickAsync();

        sut.PendingUnsavedSessions.Should().Be(0);
    }

    [Fact]
    public async Task Start_WithoutTask_Runs_ButDoesNotSaveSession()
    {
        using var sut = CreateSut();

        sut.CanStart.Should().BeTrue();
        sut.Start();
        _clock.Advance(TimeSpan.FromMinutes(25));
        await sut.TickAsync();

        sut.Phase.Should().Be(TimerPhase.ShortBreak);
        await _focus.DidNotReceiveWithAnyArgs()
            .SaveSessionAsync(default, default, default, default, default, default);
    }

    [Fact]
    public void SelectTask_WhileRunning_IsIgnored()
    {
        using var sut = CreateSut();
        sut.Start(1);

        sut.SelectTask(2);

        sut.TaskId.Should().Be(1);
    }

    [Fact]
    public void ApplySettings_WhenIdle_UpdatesDuration()
    {
        using var sut = CreateSut();
        _settings.Current.Returns(new FocusSettings { FocusMinutes = 50, AutoStartNext = false });

        sut.ApplySettings();

        sut.PhaseDuration.Should().Be(TimeSpan.FromMinutes(50));
    }
}
