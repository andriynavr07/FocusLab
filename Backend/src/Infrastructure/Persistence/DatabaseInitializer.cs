using Microsoft.EntityFrameworkCore;

namespace FocusLab.Infrastructure.Persistence;

public static class DatabaseInitializer
{
    /// <summary>
    /// Створює БД і застосовує міграції. Викликається ДО показу головного вікна.
    /// </summary>
    public static async Task InitializeAsync(IDbContextFactory<AppDbContext> factory, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);

        await db.Database.MigrateAsync(ct);

        // WAL краще працює з частими короткими записами; налаштування зберігається у файлі БД.
        await db.Database.ExecuteSqlRawAsync("PRAGMA journal_mode=WAL;", ct);
    }
}
