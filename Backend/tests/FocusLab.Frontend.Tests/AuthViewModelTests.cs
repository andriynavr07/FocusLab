using FluentAssertions;
using FocusLab.Application.Abstractions;
using Frontend.Services;
using Frontend.ViewModels;
using NSubstitute;
using Xunit;

namespace FocusLab.Frontend.Tests;

public class AuthViewModelTests
{
    private readonly IAuthService _auth = Substitute.For<IAuthService>();

    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        for (var i = 0; i < 100 && !condition(); i++)
            await Task.Delay(20);
    }

    private RegisterViewModel CreateRegister(string password = "secret1", string confirm = "secret1", ILocalizer? localizer = null)
        => new(_auth, localizer ?? TestLocalizers.Ukrainian)
        {
            Name = "Andriy",
            Email = "andriy@example.com",
            Password = password,
            ConfirmPassword = confirm
        };

    private LoginViewModel CreateLogin(Action onSuccess, ILocalizer? localizer = null)
        => new(_auth, onSuccess, localizer ?? TestLocalizers.Ukrainian)
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
        viewModel.ErrorMessage.Should().Be("Паролі не збігаються.");
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
        viewModel.ErrorMessage.Should().Be("Пароль занадто короткий.");
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
        viewModel.ErrorMessage.Should().Be("Невірна пошта або пароль.");
    }

    [Fact]
    public async Task Login_Fail_InEnglish_ShowsTranslatedError()
    {
        _auth.LoginAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(AuthResult.Fail("Невірна пошта або пароль."));
        var viewModel = CreateLogin(() => { }, TestLocalizers.English);

        viewModel.LoginCommand.Execute(null);
        await WaitUntilAsync(() => viewModel.ErrorMessage.Length > 0);

        viewModel.ErrorMessage.Should().Be("Incorrect email or password.");
    }

    [Fact]
    public void Login_CannotExecuteWithoutEmailOrPassword()
    {
        var viewModel = new LoginViewModel(_auth, () => { }, TestLocalizers.Ukrainian);

        viewModel.LoginCommand.CanExecute(null).Should().BeFalse();

        viewModel.Email = "andriy@example.com";
        viewModel.Password = "secret1";

        viewModel.LoginCommand.CanExecute(null).Should().BeTrue();
    }
}
