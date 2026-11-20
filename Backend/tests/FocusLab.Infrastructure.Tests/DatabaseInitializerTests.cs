using FluentAssertions;
using FocusLab.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace FocusLab.Infrastructure.Tests;

/// <summary>#58: створення БД і застосування міграцій при старті.</summary>
public class DatabaseInitializerTests
{
    [Fact]
    public async Task InitializeAsync_EmptyDb_NoPendingMigrations()
    {
        using var database = new TestDatabase();

        await DatabaseInitializer.InitializeAsync(database.Factory);

        await using var db = database.CreateContext();
        (await db.Database.GetPendingMigrationsAsync()).Should().BeEmpty();
        (await db.Database.GetAppliedMigrationsAsync()).Should().NotBeEmpty();
    }

    [Fact]
    public async Task InitializeAsync_CalledTwice_DoesNotFail()
    {
        using var database = new TestDatabase();
        await DatabaseInitializer.InitializeAsync(database.Factory);

        var second = () => DatabaseInitializer.InitializeAsync(database.Factory);

        await second.Should().NotThrowAsync();
    }

    [Fact]
    public async Task InitializeAsync_FileDb_SetsWalJournalMode()
    {
        // WAL не діє для :memory:, тому для цього тесту потрібен справжній файл.
        var path = Path.Combine(Path.GetTempPath(), $"focuslab-tests-{Guid.NewGuid():N}.db");
        var connectionString = $"Data Source={path};Foreign Keys=True";
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connectionString).Options;

        try
        {
            await DatabaseInitializer.InitializeAsync(new TestDbContextFactory(options));

            await using var connection = new SqliteConnection(connectionString);
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = "PRAGMA journal_mode;";
            var mode = (string?)await command.ExecuteScalarAsync();

            mode.Should().Be("wal");
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            foreach (var file in new[] { path, path + "-wal", path + "-shm" })
                if (File.Exists(file))
                    File.Delete(file);
        }
    }
}
