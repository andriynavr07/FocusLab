using FocusLab.Domain.Entities;

namespace FocusLab.Infrastructure.Tests;

public static class TestData
{
    public static readonly DateTime Now = new(2026, 11, 20, 12, 0, 0, DateTimeKind.Utc);

    public static TaskItem NewTask(string title = "Task", string? project = null) => new()
    {
        AccountId = Account.LocalAccountId,
        Title = title,
        Project = project,
        CreatedAt = Now
    };
}
