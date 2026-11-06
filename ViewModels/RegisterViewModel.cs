using System.Windows.Input;
using System.Net.Mail;
using Frontend.Services;

namespace Frontend.ViewModels;

public class RegisterViewModel : PageViewModelBase
{
    private readonly LocalAuthSession _auth;
    private string _name = string.Empty;
    private string _email = string.Empty;
    private string _password = string.Empty;
    private string _confirmPassword = string.Empty;
    private string _errorMessage = string.Empty;
    private bool _isBusy;
    private bool _isNameInvalid;
    private bool _isEmailInvalid;
    private bool _isPasswordInvalid;
    private bool _isConfirmPasswordInvalid;

    public event Action? Registered;
    public event Action? LoginRequested;

    public string Name { get => _name; set { _name = value; OnPropertyChanged(); IsNameInvalid = false; ClearError(); } }
    public string Email { get => _email; set { _email = value; OnPropertyChanged(); IsEmailInvalid = false; ClearError(); } }
    public string Password { get => _password; set { _password = value; OnPropertyChanged(); IsPasswordInvalid = false; IsConfirmPasswordInvalid = false; ClearError(); } }
    public string ConfirmPassword { get => _confirmPassword; set { _confirmPassword = value; OnPropertyChanged(); IsConfirmPasswordInvalid = false; ClearError(); } }
    public string ErrorMessage { get => _errorMessage; private set { _errorMessage = value; OnPropertyChanged(); } }
    public bool IsNameInvalid { get => _isNameInvalid; private set { _isNameInvalid = value; OnPropertyChanged(); } }
    public bool IsEmailInvalid { get => _isEmailInvalid; private set { _isEmailInvalid = value; OnPropertyChanged(); } }
    public bool IsPasswordInvalid { get => _isPasswordInvalid; private set { _isPasswordInvalid = value; OnPropertyChanged(); } }
    public bool IsConfirmPasswordInvalid { get => _isConfirmPasswordInvalid; private set { _isConfirmPasswordInvalid = value; OnPropertyChanged(); } }

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
        IsNameInvalid = string.IsNullOrWhiteSpace(Name);
        IsEmailInvalid = !IsValidEmail(Email);
        IsPasswordInvalid = string.IsNullOrEmpty(Password);
        IsConfirmPasswordInvalid = Password != ConfirmPassword;
        if (IsNameInvalid || IsEmailInvalid || IsPasswordInvalid)
        {
            ErrorMessage = "Перевірте ім’я, електронну пошту та пароль.";
            return;
        }

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

    private static bool IsValidEmail(string email) =>
        MailAddress.TryCreate(email.Trim(), out var address)
        && string.Equals(address.Address, email.Trim(), StringComparison.OrdinalIgnoreCase);

    private void ClearError()
    {
        if (string.IsNullOrEmpty(ErrorMessage)) return;
        ErrorMessage = string.Empty;
    }
}