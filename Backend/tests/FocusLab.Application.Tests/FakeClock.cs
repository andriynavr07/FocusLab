using FocusLab.Application.Abstractions;

namespace FocusLab.Application.Tests;

/// <summary>Фіксований годинник для детермінованих тестів.</summary>
public sealed class FakeClock : IClock
{
    public FakeClock(DateTime utcNow) => UtcNow = utcNow;

    public DateTime UtcNow { get; set; }
}
