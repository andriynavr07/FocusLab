using FocusLab.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace FocusLab.Infrastructure.Tests;

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
