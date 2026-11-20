using Frontend.Services;
using Xunit;
using Frontend.Services;

namespace FocusLab.Frontend.Tests;

/// <summary>Годинник із фіксованим часом.</summary>
public sealed class FakeClock(DateTime now) : IAppClock
{
    public DateTime Now { get; set; } = now;
    public DateTime Today => Now.Date;
}

/// <summary>
/// UiPreferences.Instance — спільний синглтон із глобальним станом (мова, тема).
/// Тести, які його зачіпають, мають бути в цій колекції: xUnit виконує їх послідовно.
/// </summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class UiPreferencesCollection
{
    public const string Name = "UiPreferences";
}
