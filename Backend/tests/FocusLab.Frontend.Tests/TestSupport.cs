using Frontend.Services;

namespace FocusLab.Frontend.Tests;

/// <summary>Годинник із фіксованим часом.</summary>
public sealed class FakeClock(DateTime now) : IAppClock
{
    public DateTime Now { get; set; } = now;
    public DateTime Today => Now.Date;
}

/// <summary>
/// Локалізатори з фіксованою мовою. Кожен тест отримує свій екземпляр через конструктори
/// ViewModel-ів, тож тести на тексти не залежать від UiPreferences.Instance і можуть йти паралельно.
/// </summary>
public static class TestLocalizers
{
    public static ILocalizer Ukrainian => new FixedLocalizer(FixedLocalizer.Ukrainian);
    public static ILocalizer English => new FixedLocalizer(FixedLocalizer.English);
}
