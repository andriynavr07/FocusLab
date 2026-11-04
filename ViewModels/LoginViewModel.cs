using System.Windows.Input;
using FocusLab.Application.Abstractions;

namespace frontend.ViewModels;

public class LoginViewModel : ViewModelBase
{
    private readonly IAuthService _auth;
    private string _email = string.Empty;
    private string _password = string.Empty;
    private string _errorMessage = string.Empty;
    private bool _isBusy;
    private bool _rememberMe;

    public event Action? LoggedIn;
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

    public LoginViewModel(IAuthService auth)
    {
        _auth = auth;
        LoginCommand = new RelayCommand(
            async _ => await LoginAsync(),
            _ => !_isBusy
                 && !string.IsNullOrWhiteSpace(Email)
                 && !string.IsNullOrEmpty(Password));
        NavigateToRegisterCommand = new RelayCommand(_ => RegisterRequested?.Invoke());
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
                LoggedIn?.Invoke();
            else
                ErrorMessage = result.Error ?? "Не вдалося увійти.";
        }
        catch (Exception ex)
        {
            ErrorMessage = "Помилка: " + ex.Message;
        }
        finally
        {
            _isBusy = false;
            CommandManager.InvalidateRequerySuggested();
        }
    }
}
