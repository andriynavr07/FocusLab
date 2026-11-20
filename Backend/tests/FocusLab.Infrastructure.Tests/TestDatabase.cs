using FocusLab.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace FocusLab.Infrastructure.Tests;

/// <summary>
/// SQLite in-memory: одне відкрите з'єднання на весь тест (БД живе, поки воно відкрите).
/// Foreign keys увімкнені, як у застосунку, тож каскадні видалення працюють по-справжньому.
/// </summary>
public sealed class TestDatabase : IDisposable
{
    private readonly SqliteConnection _connection;

    public TestDatabase()
    {
        _connection = new SqliteConnection("Data Source=:memory:;Foreign Keys=True");
        _connection.Open();

        Options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options;
        Factory = new TestDbContextFactory(Options);
    }

    public DbContextOptions<AppDbContext> Options { get; }

    public IDbContextFactory<AppDbContext> Factory { get; }

    public AppDbContext CreateContext() => new(Options);

    /// <summary>Накочує всі міграції — схема така сама, як у застосунку.</summary>
    public async Task MigrateAsync()
    {
        await using var db = CreateContext();
        await db.Database.MigrateAsync();
    }

    public void Dispose() => _connection.Dispose();
}

public sealed class TestDbContextFactory(DbContextOptions<AppDbContext> options) : IDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext() => new(options);

    public Task<AppDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(CreateDbContext());
}
