using System.Net.Mail;
using FocusLab.Application.Abstractions;

namespace FocusLab.Application.Services;

public class AuthService : IAuthService
{
    public const int MinPasswordLength = 8;

    private readonly IAccountRepository _accounts;
    private readonly IPasswordHasher _hasher;
    private readonly ICurrentAccount _current;

    public AuthService(IAccountRepository accounts, IPasswordHasher hasher, ICurrentAccount current)
    {
        _accounts = accounts;
        _hasher = hasher;
        _current = current;
    }

    public async Task<bool> IsRegisteredAsync(CancellationToken ct = default)
    {
        var local = await _accounts.GetLocalAsync(ct);
        return local is not null && !string.IsNullOrEmpty(local.PasswordHash);
    }

    public async Task<AuthResult> RegisterAsync(string name, string email, string password, CancellationToken ct = default)
    {
        name = name?.Trim() ?? string.Empty;
        email = NormalizeEmail(email);

        if (name.Length == 0) return AuthResult.Fail("Вкажіть ім'я.");
        if (name.Length > 100) return AuthResult.Fail("Ім'я надто довге.");
        if (!IsValidEmail(email)) return AuthResult.Fail("Некоректна адреса пошти.");
        if (string.IsNullOrEmpty(password) || password.Length < MinPasswordLength)
            return AuthResult.Fail($"Пароль має містити щонайменше {MinPasswordLength} символів.");

        var account = await _accounts.GetLocalAsync(ct);
        if (account is null) return AuthResult.Fail("Локальний акаунт не знайдено. Перевірте міграції бази.");
        if (!string.IsNullOrEmpty(account.PasswordHash)) return AuthResult.Fail("Акаунт уже створено. Увійдіть.");

        account.Username = name;
        account.Email = email;
        account.PasswordHash = _hasher.Hash(password);
        await _accounts.UpdateAsync(account, ct);

        _current.SignIn(account.Id);
        return AuthResult.Ok();
    }

    public async Task<AuthResult> LoginAsync(string email, string password, CancellationToken ct = default)
    {
        var account = await _accounts.GetByEmailAsync(NormalizeEmail(email), ct);

        if (account is null
            || string.IsNullOrEmpty(account.PasswordHash)
            || !_hasher.Verify(password ?? string.Empty, account.PasswordHash))
            return AuthResult.Fail("Невірна пошта або пароль.");

        _current.SignIn(account.Id);
        return AuthResult.Ok();
    }

    private static string NormalizeEmail(string? email) => (email ?? string.Empty).Trim().ToLowerInvariant();

    private static bool IsValidEmail(string email) =>
        MailAddress.TryCreate(email, out var addr) && addr.Address == email;
}