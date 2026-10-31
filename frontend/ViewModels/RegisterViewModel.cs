using System;
using System.Windows.Input;

namespace frontend.ViewModels;

public class RegisterViewModel : ViewModelBase
{
    private string _name;
    private string _email;
    private string _password;
    private string _confirmPassword;

    public string Name
    {
        get => _name;
        set { _name = value; OnPropertyChanged(); }
    }

    public string Email
    {
        get => _email;
        set { _email = value; OnPropertyChanged(); }
    }

    public string Password
    {
        get => _password;
        set { _password = value; OnPropertyChanged(); }
    }

    public string ConfirmPassword
    {
        get => _confirmPassword;
        set { _confirmPassword = value; OnPropertyChanged(); }
    }


    public ICommand RegisterCommand { get; }
    public ICommand NavigateToLoginCommand { get; }

    // 1. Додаємо конструктор за замовчуванням для XAML-парсетра
    public RegisterViewModel()
    {
        RegisterCommand = new RelayCommand(obj => 
        {
            // Логіка реєстрації
        }, obj => !string.IsNullOrEmpty(Email) && !string.IsNullOrEmpty(Password));
    }

    // 2. Конструктор з параметром для ручного виклику (якщо знадобиться)
    public RegisterViewModel(Action navigateToLogin) : this()
    {
        NavigateToLoginCommand = new RelayCommand(obj => navigateToLogin?.Invoke());
    }
}