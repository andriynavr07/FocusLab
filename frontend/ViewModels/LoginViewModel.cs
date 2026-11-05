using System.Windows.Input;
using Frontend.Services;

namespace Frontend.ViewModels;

public class LoginViewModel : ViewModelBase
{
    private readonly LocalAuthSession _auth;
    private readonly Action _onLoginSuccess;
    private string _email = string.Empty;
    private string _password = string.Empty;
    private string _errorMessage = string.Empty;
    private bool _isBusy;
    private bool _rememberMe;

    public event Action? RegisterRequested;

    public string Email
    {
        get => _email;
        set
        {
            _email = value;
            OnPropertyChanged();
            CommandManager.InvalidateRequerySuggested();
        }
    }

    public string Password
    {
        get => _password;
        set
        {
            _password = value;
            OnPropertyChanged();
            CommandManager.InvalidateRequerySuggested();
        }
    }

    public string ErrorMessage
    {
        get => _errorMessage;
        private set { _errorMessage = value; OnPropertyChanged(); }
    }

    public bool RememberMe
    {
        get => _rememberMe;
        set { _rememberMe = value; OnPropertyChanged(); }
    }

    public ICommand LoginCommand { get; }
    public ICommand NavigateToRegisterCommand { get; }

    public LoginViewModel(LocalAuthSession auth, Action onLoginSuccess)
    {
        _auth = auth;
        _onLoginSuccess = onLoginSuccess ?? throw new ArgumentNullException(nameof(onLoginSuccess));
        LoginCommand = new RelayCommand(
            _ => Login(),
            _ => !_isBusy
                 && !string.IsNullOrWhiteSpace(Email)
                 && !string.IsNullOrEmpty(Password));
        NavigateToRegisterCommand = new RelayCommand(_ => RegisterRequested?.Invoke());
    }

    private void Login()
    {
        ErrorMessage = string.Empty;
        _isBusy = true;
        CommandManager.InvalidateRequerySuggested();

        try
        {
            if (_auth.TryLogin(Email, Password))
                _onLoginSuccess();
            else
                ErrorMessage = "Невірна пошта або пароль. Зареєструйтеся в цьому запуску застосунку.";
        }
        finally
        {
            _isBusy = false;
            CommandManager.InvalidateRequerySuggested();
        }
    }
}
