using FluentAssertions;
using FocusLab.Infrastructure.Persistence;

namespace FocusLab.Infrastructure.Tests;

/// <summary>#57: дати в БД зберігаються в UTC.</summary>
public class UtcDateTimeConverterTests
{
    [Fact]
    public void LocalTime_IsStoredAsUtc()
    {
        var converter = new UtcDateTimeConverter();
        var local = new DateTime(2026, 7, 15, 12, 0, 0, DateTimeKind.Local);

        var stored = (DateTime)converter.ConvertToProvider(local)!;

        stored.Kind.Should().Be(DateTimeKind.Utc);
        stored.Should().Be(local.ToUniversalTime());
    }

    [Fact]
    public void ReadValue_HasUtcKind()
    {
        var converter = new UtcDateTimeConverter();
        // SQLite повертає DateTime з Kind = Unspecified
        var raw = new DateTime(2026, 11, 20, 10, 0, 0, DateTimeKind.Unspecified);

        var read = (DateTime)converter.ConvertFromProvider(raw)!;

        read.Kind.Should().Be(DateTimeKind.Utc);
        read.Ticks.Should().Be(raw.Ticks);
    }

    [Fact]
    public void Nullable_NullStaysNull()
    {
        var converter = new NullableUtcDateTimeConverter();

        converter.ConvertToProvider(null).Should().BeNull();
        converter.ConvertFromProvider(null).Should().BeNull();
    }
}
