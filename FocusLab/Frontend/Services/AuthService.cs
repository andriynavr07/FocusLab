namespace frontend.Services;

public interface IAuthService
{
    Task<AuthResult> RegisterAsync(string name, string email, string password);
    Task<bool> IsRegisteredAsync();
    Task<bool> LoginAsync(string email, string password);
}

public sealed record AuthResult(bool Success, string? Error = null);

public class AuthService : IAuthService
{
    private readonly object _sync = new();
    private bool _isRegistered;
    private string? _registeredEmail;
    private string? _registeredPassword;

    public Task<AuthResult> RegisterAsync(string name, string email, string password)
    {
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            return Task.FromResult(new AuthResult(false, "Всі поля повинні бути заповнені."));
        }

        if (!email.Contains('@'))
        {
            return Task.FromResult(new AuthResult(false, "Некоректна електронна пошта."));
        }

        lock (_sync)
        {
            _isRegistered = true;
            _registeredEmail = email;
            _registeredPassword = password;
        }

        return Task.FromResult(new AuthResult(true));
    }

    public Task<bool> IsRegisteredAsync()
    {
        lock (_sync)
        {
            return Task.FromResult(_isRegistered);
        }
    }

    public Task<bool> LoginAsync(string email, string password)
    {
        lock (_sync)
        {
            return Task.FromResult(
                _isRegistered
                && string.Equals(_registeredEmail, email, StringComparison.OrdinalIgnoreCase)
                && _registeredPassword == password);
        }
    }
}
