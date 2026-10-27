using FocusLab.Application.Abstractions;

namespace FocusLab.Infrastructure.SystemServices;

public sealed class SystemClock : IClock
{
    public DateTime UtcNow => DateTime.UtcNow;
}
