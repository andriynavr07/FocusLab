namespace FocusLab.Application.Abstractions;

public sealed record AuthResult(bool Success, string? Error = null)
{
    public static AuthResult Ok() => new(true);
    public static AuthResult Fail(string error) => new(false, error);
}

public interface IAuthService
{
    Task<bool> IsRegisteredAsync(CancellationToken ct = default);
    Task<AuthResult> RegisterAsync(string name, string email, string password, CancellationToken ct = default);
    Task<AuthResult> LoginAsync(string email, string password, CancellationToken ct = default);
}