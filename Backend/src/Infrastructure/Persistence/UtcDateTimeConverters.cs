using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace FocusLab.Infrastructure.Persistence;

/// <summary>Гарантує, що DateTime у БД зберігається в UTC, а читається з Kind = Utc.</summary>
public sealed class UtcDateTimeConverter : ValueConverter<DateTime, DateTime>
{
    public UtcDateTimeConverter()
        : base(v => v.ToUniversalTime(), v => DateTime.SpecifyKind(v, DateTimeKind.Utc))
    {
    }
}

public sealed class NullableUtcDateTimeConverter : ValueConverter<DateTime?, DateTime?>
{
    public NullableUtcDateTimeConverter()
        : base(
            v => v.HasValue ? v.Value.ToUniversalTime() : v,
            v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : v)
    {
    }
}
