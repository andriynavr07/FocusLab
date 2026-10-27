namespace FocusLab.Infrastructure.SystemServices;

/// <summary>Єдине місце, де визначено, де застосунок зберігає свої дані: %AppData%\FocusLab\.</summary>
public static class AppPaths
{
    public static string Root { get; } =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "FocusLab");

    public static string LogsDirectory { get; } = Path.Combine(Root, "logs");

    public static string DatabaseFile { get; } = Path.Combine(Root, "focuslab.db");

    public static string SettingsFile { get; } = Path.Combine(Root, "settings.json");

    /// <summary>Єдиний рядок підключення до SQLite.</summary>
    public static string ConnectionString { get; } = $"Data Source={DatabaseFile};Foreign Keys=True";

    /// <summary>Створює каталог даних, logs і порожній settings.json, якщо їх ще немає.</summary>
    public static void EnsureCreated()
    {
        Directory.CreateDirectory(Root);
        Directory.CreateDirectory(LogsDirectory);

        if (!File.Exists(SettingsFile))
            File.WriteAllText(SettingsFile, "{}");
    }
}
