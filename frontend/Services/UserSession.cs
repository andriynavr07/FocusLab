namespace Frontend.Services;

/// <summary>
/// Дані поточного користувача для UI: ім'я й пошта з БД (задаються під час реєстрації)
/// та серійний період (кількість днів активності поспіль). Одиночка, бо ці дані показуються
/// на кількох сторінках одночасно (верхня панель, налаштування, головна).
/// Сторінки підписуються на Changed через WeakEventManager, тому не утримуються в пам'яті.
/// </summary>
public sealed class UserSession
{
    public static UserSession Instance { get; } = new();

    private UserSession() { }

    public string Name { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public int StreakDays { get; private set; }

    public event EventHandler<EventArgs>? Changed;

    public string Initials
    {
        get
        {
            var parts = Name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var letters = string.Concat(parts.Take(2).Select(p => char.ToUpperInvariant(p[0])));
            return letters.Length > 0 ? letters : "?";
        }
    }

    public string StreakText
        => $"{StreakDays} {Uk.Plural(StreakDays, "день", "дні", "днів")} активності";

    public void SetProfile(string name, string email)
    {
        Name = name ?? string.Empty;
        Email = email ?? string.Empty;
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public void SetStreak(int days)
    {
        if (StreakDays == days) return;
        StreakDays = Math.Max(0, days);
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public void Clear()
    {
        Name = string.Empty;
        Email = string.Empty;
        StreakDays = 0;
        Changed?.Invoke(this, EventArgs.Empty);
    }
}
