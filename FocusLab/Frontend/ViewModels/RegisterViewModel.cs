using System.Windows.Input;
using frontend.Services;

namespace frontend.ViewModels;

public class RegisterViewModel : ViewModelBase
{
    private readonly frontend.Services.IAuthService _auth;
    private string _name = string.Empty;
    private string _email = string.Empty;
    private string _password = string.Empty;
    private string _confirmPassword = string.Empty;
    private string _errorMessage = string.Empty;
    private bool _isBusy;

    public event Action? Registered;
    public event Action? LoginRequested;

    public string Name { get => _name; set { _name = value; OnPropertyChanged(); } }
    public string Email { get => _email; set { _email = value; OnPropertyChanged(); } }
    public string Password { get => _password; set { _password = value; OnPropertyChanged(); } }
    public string ConfirmPassword { get => _confirmPassword; set { _confirmPassword = value; OnPropertyChanged(); } }
    public string ErrorMessage { get => _errorMessage; private set { _errorMessage = value; OnPropertyChanged(); } }

    public ICommand RegisterCommand { get; }
    public ICommand NavigateToLoginCommand { get; }

    public RegisterViewModel(frontend.Services.IAuthService auth)
    {
        _auth = auth;
        RegisterCommand = new RelayCommand(
            async _ => await RegisterAsync(),
            _ => !_isBusy
                 && !string.IsNullOrWhiteSpace(Name)
                 && !string.IsNullOrWhiteSpace(Email)
                 && !string.IsNullOrEmpty(Password));
        NavigateToLoginCommand = new RelayCommand(_ => LoginRequested?.Invoke());
    }

    private async Task RegisterAsync()
    {
        ErrorMessage = string.Empty;
        if (Password != ConfirmPassword)
        {
            ErrorMessage = "Паролі не збігаються.";
            return;
        }

        _isBusy = true;
        try
        {
            var result = await _auth.RegisterAsync(Name, Email, Password);
            if (result.Success) Registered?.Invoke();
            else ErrorMessage = result.Error ?? "Не вдалося зареєструватися.";
        }
        catch (Exception ex)
        {
            ErrorMessage = "Помилка: " + ex.Message;
        }
        finally
        {
            _isBusy = false;
        }
    }
}