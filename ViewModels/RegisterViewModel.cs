using System.Windows.Input;
using Frontend.Services;

namespace Frontend.ViewModels;

public class RegisterViewModel : ViewModelBase
{
    private readonly LocalAuthSession _auth;
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

    public RegisterViewModel(LocalAuthSession auth)
    {
        _auth = auth;
        RegisterCommand = new RelayCommand(
            _ => Register(),
            _ => !_isBusy
                 && !string.IsNullOrWhiteSpace(Name)
                 && !string.IsNullOrWhiteSpace(Email)
                 && !string.IsNullOrEmpty(Password));
        NavigateToLoginCommand = new RelayCommand(_ => LoginRequested?.Invoke());
    }

    private void Register()
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
            if (_auth.TryRegister(Name, Email, Password, out var error))
                Registered?.Invoke();
            else
                ErrorMessage = error;
        }
        finally { _isBusy = false; }
    }
}