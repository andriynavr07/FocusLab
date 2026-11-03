using FocusLab.Application.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace frontend.ViewModels;

public class MainViewModel : ViewModelBase
{
    private readonly IServiceProvider _sp;
    private ViewModelBase? _current;

    public ViewModelBase? Current
    {
        get => _current;
        private set { _current = value; OnPropertyChanged(); }
    }

    public MainViewModel(IServiceProvider sp) => _sp = sp;

    public async Task StartAsync()
    {
        var auth = _sp.GetRequiredService<IAuthService>();
        if (await auth.IsRegisteredAsync()) ShowHome(); // TODO: тут буде екран логіну
        else ShowRegister();
    }

    private void ShowRegister()
    {
        var vm = _sp.GetRequiredService<RegisterViewModel>();
        vm.Registered += ShowHome;
        Current = vm;
    }

    private void ShowHome() => Current = _sp.GetRequiredService<HomeViewModel>();
}