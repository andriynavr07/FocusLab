using System.Windows.Input;
using System.Windows;
using Frontend.Services;

namespace Frontend.ViewModels;

public class LoginViewModel : PageViewModelBase
{
    private readonly LocalAuthSession _auth;
    private readonly Action _onLoginSuccess;
    private string _email = string.Empty;
    private string _password = string.Empty;
    private string _errorMessage = string.Empty;
    private bool _isBusy;
    private bool _rememberMe;
    private bool _isEmailInvalid;
    private bool _isPasswordInvalid;

    public event Action? RegisterRequested;

    public string Email
    {
        get => _email;
        set
        {
            _email = value;
            OnPropertyChanged();
            IsEmailInvalid = false;
            ClearError();
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
            IsPasswordInvalid = false;
            ClearError();
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

    public bool IsEmailInvalid { get => _isEmailInvalid; private set { _isEmailInvalid = value; OnPropertyChanged(); } }
    public bool IsPasswordInvalid { get => _isPasswordInvalid; private set { _isPasswordInvalid = value; OnPropertyChanged(); } }

    public ICommand LoginCommand { get; }
    public ICommand NavigateToRegisterCommand { get; }
    public ICommand ForgotPasswordCommand { get; }

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
        ForgotPasswordCommand = new RelayCommand(_ =>
            MessageBox.Show(
                "Відновлення пароля недоступне для локального акаунта. Зареєструйте акаунт або використайте пароль, збережений під час реєстрації.",
                "Відновлення пароля",
                MessageBoxButton.OK,
                MessageBoxImage.Information));
    }

    private void Login()
    {
        ErrorMessage = string.Empty;
        IsEmailInvalid = false;
        IsPasswordInvalid = false;
        _isBusy = true;
        CommandManager.InvalidateRequerySuggested();

        try
        {
            if (_auth.TryLogin(Email, Password))
                _onLoginSuccess();
            else
            {
                ErrorMessage = "Невірна пошта або пароль. Зареєструйтеся в цьому запуску застосунку.";
                IsEmailInvalid = true;
                IsPasswordInvalid = true;
            }
        }
        finally
        {
            _isBusy = false;
            CommandManager.InvalidateRequerySuggested();
        }
    }

    private void ClearError()
    {
        if (string.IsNullOrEmpty(ErrorMessage)) return;
        ErrorMessage = string.Empty;
    }
}
