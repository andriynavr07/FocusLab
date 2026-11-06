namespace FocusLab.Application.Abstractions;

public sealed record AuthResult(bool Success, string? Error = null)
{
    public static AuthResult Ok() => new(true);
    public static AuthResult Fail(string error) => new(false, error);
}

/// <summary>Ім'я та пошта поточного користувача (зберігаються в БД під час реєстрації).</summary>
public sealed record AccountProfile(string Name, string Email);

public interface IAuthService
{
    Task<bool> IsRegisteredAsync(CancellationToken ct = default);
    Task<AuthResult> RegisterAsync(string name, string email, string password, CancellationToken ct = default);
    Task<AuthResult> LoginAsync(string email, string password, CancellationToken ct = default);

    /// <summary>Профіль залогіненого користувача; null, якщо ніхто не увійшов.</summary>
    Task<AccountProfile?> GetCurrentProfileAsync(CancellationToken ct = default);

    /// <summary>Оновлює ім'я та пошту поточного користувача в БД.</summary>
    Task<AuthResult> UpdateProfileAsync(string name, string email, CancellationToken ct = default);
}