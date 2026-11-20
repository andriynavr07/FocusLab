using FluentAssertions;
using Frontend.ViewModels;
using Xunit;

namespace FocusLab.Frontend.Tests;

public class RelayCommandTests
{
    [Fact]
    public void Execute_InvokesAction()
    {
        var calls = 0;
        var command = new RelayCommand(_ => calls++);

        command.Execute(null);

        calls.Should().Be(1);
    }

    [Fact]
    public void Execute_PassesParameterToAction()
    {
        object? received = null;
        var command = new RelayCommand(p => received = p);

        command.Execute("task");

        received.Should().Be("task");
    }

    [Fact]
    public void CanExecute_WithoutPredicate_IsAlwaysTrue()
    {
        var command = new RelayCommand(_ => { });

        command.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public void CanExecute_UsesPredicate()
    {
        var command = new RelayCommand(_ => { }, p => p is "ok");

        command.CanExecute("ok").Should().BeTrue();
        command.CanExecute("no").Should().BeFalse();
        command.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void CanExecute_ReflectsChangesOfExternalState()
    {
        var enabled = false;
        var command = new RelayCommand(_ => { }, _ => enabled);

        command.CanExecute(null).Should().BeFalse();

        enabled = true;

        command.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public void Constructor_WithoutAction_Throws()
    {
        var create = () => new RelayCommand(null!);

        create.Should().Throw<ArgumentNullException>();
    }
}
