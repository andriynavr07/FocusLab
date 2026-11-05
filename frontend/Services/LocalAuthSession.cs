namespace Frontend.Services;

public sealed class LocalAuthSession
{
    private string? _email;
    private string? _password;

    public bool HasAccount => _email is not null;

    public bool TryRegister(string name, string email, string password, out string error)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            error = "Вкажіть ім'я.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            error = "Вкажіть електронну пошту.";
            return false;
        }

        if (string.IsNullOrEmpty(password))
        {
            error = "Вкажіть пароль.";
            return false;
        }

        _email = email.Trim();
        _password = password;
        error = string.Empty;
        return true;
    }

    public bool TryLogin(string email, string password) =>
        _email is not null
        && _password is not null
        && string.Equals(_email, email.Trim(), StringComparison.OrdinalIgnoreCase)
        && string.Equals(_password, password, StringComparison.Ordinal);
}
