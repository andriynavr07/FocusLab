using Frontend.Services;

namespace Frontend.ViewModels;

public class MainViewModel : ViewModelBase
{
    private readonly LocalAuthSession _auth;
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

    public MainViewModel(LocalAuthSession auth) => _auth = auth;

    public void Start()
    {
        if (_auth.HasAccount)
            ShowLogin();
        else
            ShowRegister();
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

    private void ShowWorkspace() => Current = new WorkspaceViewModel(onLogout: ShowLogin);
}
