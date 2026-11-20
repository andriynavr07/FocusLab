namespace Frontend.Services;

/// <summary>Поточний локальний час. Через інтерфейс його можна підміняти в тестах.</summary>
public interface IAppClock
{
    DateTime Now { get; }
    DateTime Today { get; }
}

public sealed class AppClock : IAppClock
{
    public static AppClock Instance { get; } = new();

    public DateTime Now => DateTime.Now;
    public DateTime Today => DateTime.Today;
}
