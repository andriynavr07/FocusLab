using System.Windows.Controls;
using frontend.Services;

namespace frontend.ViewModels;

public class LoginViewModel : ViewModelBase
{
    private readonly frontend.Services.IAuthService _auth;
    private readonly Action _onLoginSuccess;
    private readonly Action _goToRegister;

    private string _email = "";
    public string Email
    {
        get => _email;
        set { _email = value; OnPropertyChanged(); }
    }

    private string _errorMessage = "";
    public string ErrorMessage
    {
        get => _errorMessage;
        set { _errorMessage = value; OnPropertyChanged(); }
    }

    private bool _isBusy;
    public bool IsBusy
    {
        get => _isBusy;
        set { _isBusy = value; OnPropertyChanged(); }
    }

    public RelayCommand LoginCommand { get; }
    public RelayCommand GoToRegisterCommand { get; }

    public LoginViewModel(frontend.Services.IAuthService auth, Action onLoginSuccess, Action goToRegister)
    {
        _auth = auth;
        _onLoginSuccess = onLoginSuccess;
        _goToRegister = goToRegister;

        LoginCommand = new RelayCommand(async p => await LoginAsync(p));
        GoToRegisterCommand = new RelayCommand(_ => _goToRegister());
    }

    private async Task LoginAsync(object? parameter)
    {
        var password = (parameter as PasswordBox)?.Password ?? "";

        if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(password))
        {
            ErrorMessage = "Введіть email і пароль";
            return;
        }

        IsBusy = true;
        ErrorMessage = "";

        try
        {
            var ok = await _auth.LoginAsync(Email, password);
            if (ok) _onLoginSuccess();
            else ErrorMessage = "Невірний email або пароль";
        }
        catch (Exception ex)
        {
            ErrorMessage = "Помилка входу: " + ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }
}