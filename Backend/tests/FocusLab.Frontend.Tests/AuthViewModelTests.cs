using FluentAssertions;
using FocusLab.Application.Abstractions;
using Frontend.Services;
using Frontend.ViewModels;
using NSubstitute;
using Xunit;

namespace FocusLab.Frontend.Tests;

[Collection(UiPreferencesCollection.Name)]
public class AuthViewModelTests
{
    private readonly IAuthService _auth = Substitute.For<IAuthService>();

    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        for (var i = 0; i < 100 && !condition(); i++)
            await Task.Delay(20);
    }

    private RegisterViewModel CreateRegister(string password = "secret1", string confirm = "secret1")
        => new(_auth)
        {
            Name = "Andriy",
            Email = "andriy@example.com",
            Password = password,
            ConfirmPassword = confirm
        };

    private LoginViewModel CreateLogin(Action onSuccess)
        => new(_auth, onSuccess)
        {
            Email = "andriy@example.com",
            Password = "secret1"
        };

    // ───── Register ─────

    [Fact]
    public async Task Register_PasswordsMismatch_DoesNotCallAuth()
    {
        var viewModel = CreateRegister(password: "secret1", confirm: "different");
        var registered = false;
        viewModel.Registered += () => registered = true;

        viewModel.RegisterCommand.Execute(null);
        await WaitUntilAsync(() => viewModel.ErrorMessage.Length > 0);

        await _auth.DidNotReceiveWithAnyArgs().RegisterAsync(default!, default!, default!);
        registered.Should().BeFalse();
        viewModel.ErrorMessage.Should().Be(UiPreferences.Instance.Translate("Паролі не збігаються."));
    }

    [Fact]
    public async Task Register_Success_RaisesRegistered()
    {
        _auth.RegisterAsync("Andriy", "andriy@example.com", "secret1", Arg.Any<CancellationToken>())
            .Returns(AuthResult.Ok());
        var viewModel = CreateRegister();
        var registered = 0;
        viewModel.Registered += () => registered++;

        viewModel.RegisterCommand.Execute(null);
        await WaitUntilAsync(() => registered > 0);

        registered.Should().Be(1);
        viewModel.ErrorMessage.Should().BeEmpty();
        await _auth.Received(1).RegisterAsync("Andriy", "andriy@example.com", "secret1", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Register_AuthFail_ShowsError()
    {
        _auth.RegisterAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(AuthResult.Fail("Пароль занадто короткий."));
        var viewModel = CreateRegister();
        var registered = false;
        viewModel.Registered += () => registered = true;

        viewModel.RegisterCommand.Execute(null);
        await WaitUntilAsync(() => viewModel.ErrorMessage.Length > 0);

        registered.Should().BeFalse();
        viewModel.ErrorMessage.Should().Be(UiPreferences.Instance.Translate("Пароль занадто короткий."));
    }

    // ───── Login ─────

    [Fact]
    public async Task Login_Success_InvokesCallback()
    {
        _auth.LoginAsync("andriy@example.com", "secret1", Arg.Any<CancellationToken>())
            .Returns(AuthResult.Ok());
        var calls = 0;
        var viewModel = CreateLogin(() => calls++);

        viewModel.LoginCommand.Execute(null);
        await WaitUntilAsync(() => calls > 0);

        calls.Should().Be(1);
        viewModel.ErrorMessage.Should().BeEmpty();
    }

    [Fact]
    public async Task Login_Fail_ShowsError()
    {
        _auth.LoginAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(AuthResult.Fail("Невірна пошта або пароль."));
        var calls = 0;
        var viewModel = CreateLogin(() => calls++);

        viewModel.LoginCommand.Execute(null);
        await WaitUntilAsync(() => viewModel.ErrorMessage.Length > 0);

        calls.Should().Be(0);
        viewModel.ErrorMessage.Should().Be(UiPreferences.Instance.Translate("Невірна пошта або пароль."));
    }

    [Fact]
    public void Login_CannotExecuteWithoutEmailOrPassword()
    {
        var viewModel = new LoginViewModel(_auth, () => { });

        viewModel.LoginCommand.CanExecute(null).Should().BeFalse();

        viewModel.Email = "andriy@example.com";
        viewModel.Password = "secret1";

        viewModel.LoginCommand.CanExecute(null).Should().BeTrue();
    }
}
