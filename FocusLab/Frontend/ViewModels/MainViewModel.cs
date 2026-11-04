using frontend.Services;
using Microsoft.Extensions.DependencyInjection;

namespace frontend.ViewModels;

public class MainViewModel : ViewModelBase
{
    private readonly IServiceProvider _sp;
    private readonly frontend.Services.IAuthService _auth;
    private ViewModelBase? _current;

    public ViewModelBase? Current
    {
        get => _current;
        private set { _current = value; OnPropertyChanged(); }
    }

    public MainViewModel(IServiceProvider sp, frontend.Services.IAuthService auth)
    {
        _sp = sp;
        _auth = auth;
    }

    public async Task StartAsync()
    {
        if (await _auth.IsRegisteredAsync())
        {
            ShowHome();
            return;
        }

        ShowRegister();
    }

    private void ShowRegister()
    {
        var vm = _sp.GetRequiredService<RegisterViewModel>();
        vm.Registered += ShowHome;
        vm.LoginRequested += ShowLogin;
        Current = vm;
    }

    private void ShowLogin()
    {
        var vm = ActivatorUtilities.CreateInstance<LoginViewModel>(
            _sp,
            (Action)ShowHome,
            (Action)ShowRegister);
        Current = vm;
    }

    private void ShowHome() => Current = _sp.GetRequiredService<HomeViewModel>();
}