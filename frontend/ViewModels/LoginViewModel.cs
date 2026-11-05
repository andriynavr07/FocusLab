using System.Windows;
using System.Windows.Input;
using FocusLab.Application.Abstractions;

namespace Frontend.ViewModels;

public class LoginViewModel : PageViewModelBase
{
    private readonly IAuthService _auth;
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
    public ICommand ForgotPasswordCommand { get; }

    public LoginViewModel(IAuthService auth, Action onLoginSuccess)
    {
        _auth = auth;
        _onLoginSuccess = onLoginSuccess ?? throw new ArgumentNullException(nameof(onLoginSuccess));
        LoginCommand = new RelayCommand(
            async _ => await LoginAsync(),
            _ => !_isBusy
                 && !string.IsNullOrWhiteSpace(Email)
                 && !string.IsNullOrEmpty(Password));
        NavigateToRegisterCommand = new RelayCommand(_ => RegisterRequested?.Invoke());
        ForgotPasswordCommand = new RelayCommand(_ =>
            MessageBox.Show(
                "Відновлення пароля недоступне для локального акаунта.",
                "Відновлення пароля",
                MessageBoxButton.OK,
                MessageBoxImage.Information));
    }

    private async Task LoginAsync()
    {
        ErrorMessage = string.Empty;
        _isBusy = true;
        CommandManager.InvalidateRequerySuggested();

        try
        {
            var result = await _auth.LoginAsync(Email, Password);
            if (result.Success)
                _onLoginSuccess();
            else
                ErrorMessage = result.Error ?? "Невірна пошта або пароль.";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Помилка входу: {ex.Message}";
        }
        finally
        {
            _isBusy = false;
            CommandManager.InvalidateRequerySuggested();
        }
    }
}
