using System.Windows;
using FocusLab.Application.Abstractions;
using FocusLab.Application.Focus;
using Frontend.Services;

namespace Frontend.ViewModels;

public class MainViewModel : ViewModelBase
{
    private readonly IAuthService _auth;
    private readonly ITaskService _tasks;
    private readonly ICurrentAccount _currentAccount;
    private readonly IFocusTimerService _focusTimer;
    private readonly IFocusSettingsProvider _focusSettings;
    private ViewModelBase? _current;

    public ViewModelBase? Current
    {
        get => _current;
        private set
        {
            _current = value;
            OnPropertyChanged();
        }
    }

    public MainViewModel(
        IAuthService auth,
        ITaskService tasks,
        ICurrentAccount currentAccount,
        IFocusTimerService focusTimer,
        IFocusSettingsProvider focusSettings)
    {
        _auth = auth;
        _tasks = tasks;
        _currentAccount = currentAccount;
        _focusTimer = focusTimer;
        _focusSettings = focusSettings;
    }

    public async void Start()
    {
        try
        {
            if (await _auth.IsRegisteredAsync())
                ShowLogin();
            else
                ShowRegister();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Не вдалося прочитати акаунт:\n{ex.Message}", "FocusLab",
                MessageBoxButton.OK, MessageBoxImage.Error);
            ShowLogin();
        }
    }

    private void ShowRegister()
    {
        var viewModel = new RegisterViewModel(_auth);
        viewModel.Registered += ShowWorkspace;
        viewModel.LoginRequested += ShowLogin;
        Current = viewModel;
    }

    private void ShowLogin()
    {
        var viewModel = new LoginViewModel(_auth, ShowWorkspace);
        viewModel.RegisterRequested += ShowRegister;
        Current = viewModel;
    }

    private async void ShowWorkspace()
    {
        var store = new TaskBoardStore(_tasks);
        var timer = new TimerViewModel(_focusTimer, _focusSettings);
        Current = new WorkspaceViewModel(store, timer, onLogout: Logout);
        await store.LoadAsync();
    }

    private void Logout()
    {
        _currentAccount.SignOut();
        ShowLogin();
    }
}
