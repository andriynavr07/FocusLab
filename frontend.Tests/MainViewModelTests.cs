using System.Reflection;
using FluentAssertions;
using FocusLab.Application.Abstractions;
using FocusLab.Application.Focus;
using Frontend.ViewModels;
using NSubstitute;

namespace Frontend.Tests;

/// <summary>#75: навігація між екранами входу, реєстрації та виходу.</summary>
public class MainViewModelTests
{
    private readonly IAuthService _auth = Substitute.For<IAuthService>();
    private readonly ICurrentAccount _currentAccount = Substitute.For<ICurrentAccount>();

    private MainViewModel CreateViewModel() => new(
        _auth,
        Substitute.For<ITaskService>(),
        Substitute.For<ICatalogService>(),
        _currentAccount,
        Substitute.For<IFocusTimerService>(),
        Substitute.For<IFocusSettingsProvider>(),
        Substitute.For<IActivityService>());

    [Fact]
    public void Start_Registered_ShowsLogin()
    {
        _auth.IsRegisteredAsync(Arg.Any<CancellationToken>()).Returns(true);
        var viewModel = CreateViewModel();

        // Start() — async void, але підроблений сервіс повертає вже завершену Task,
        // тому await всередині виконується синхронно і Current встановлюється одразу.
        viewModel.Start();

        viewModel.Current.Should().BeOfType<LoginViewModel>();
    }

    [Fact]
    public void Start_NotRegistered_ShowsRegister()
    {
        _auth.IsRegisteredAsync(Arg.Any<CancellationToken>()).Returns(false);
        var viewModel = CreateViewModel();

        viewModel.Start();

        viewModel.Current.Should().BeOfType<RegisterViewModel>();
    }

    [Fact]
    public void Logout_ReturnsToLogin()
    {
        _auth.IsRegisteredAsync(Arg.Any<CancellationToken>()).Returns(false);
        var viewModel = CreateViewModel();
        viewModel.Start();
        viewModel.Current.Should().BeOfType<RegisterViewModel>();

        // Logout() приватний: у застосунку його викликає LogoutCommand робочої області,
        // але створення WorkspaceViewModel потребує таймера та UI-ресурсів.
        typeof(MainViewModel)
            .GetMethod("Logout", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(viewModel, null);

        _currentAccount.Received(1).SignOut();
        viewModel.Current.Should().BeOfType<LoginViewModel>();
    }
}
