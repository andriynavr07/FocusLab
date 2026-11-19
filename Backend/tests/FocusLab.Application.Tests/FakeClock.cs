using FocusLab.Application.Abstractions;

namespace FocusLab.Application.Tests;

public sealed class FakeClock : IClock
{
    public DateTime UtcNow { get; set; } = new(2026, 11, 4, 9, 0, 0, DateTimeKind.Utc);

    public void Advance(TimeSpan by) => UtcNow += by;
}
