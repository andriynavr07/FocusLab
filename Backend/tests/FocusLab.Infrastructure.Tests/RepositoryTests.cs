using FluentAssertions;
using FocusLab.Domain.Entities;
using FocusLab.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FocusLab.Infrastructure.Tests;

public class RepositoryTests
{
    private static async Task<int> AddTagAsync(TestDatabase database, string name)
    {
        await using var db = database.CreateContext();
        var tag = new Tag { AccountId = Account.LocalAccountId, Name = name, Color = "#3B6EF5" };
        db.Tags.Add(tag);
        await db.SaveChangesAsync();
        return tag.Id;
    }

    [Fact]
    public async Task SetTags_UnknownTag_Throws()
    {
        using var database = new TestDatabase();
        await database.MigrateAsync();
        var repository = new TaskRepository(database.Factory);
        var task = TestData.NewTask();
        await repository.AddAsync(task);

        var setTags = () => repository.SetTagsAsync(task.Id, new[] { 999 });

        await setTags.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task SetTags_ReplacesExisting()
    {
        using var database = new TestDatabase();
        await database.MigrateAsync();
        var first = await AddTagAsync(database, "First");
        var second = await AddTagAsync(database, "Second");
        var third = await AddTagAsync(database, "Third");
        var repository = new TaskRepository(database.Factory);
        var task = TestData.NewTask();
        await repository.AddAsync(task);

        await repository.SetTagsAsync(task.Id, new[] { first, second });
        await repository.SetTagsAsync(task.Id, new[] { third });

        var loaded = await repository.GetByIdAsync(task.Id);
        loaded!.TaskTags.Select(tt => tt.TagId).Should().BeEquivalentTo(new[] { third });
    }

    [Fact]
    public async Task Update_UnknownId_Throws()
    {
        using var database = new TestDatabase();
        await database.MigrateAsync();
        var repository = new TaskRepository(database.Factory);
        var missing = TestData.NewTask();
        missing.Id = 999;

        var update = () => repository.UpdateAsync(missing);

        await update.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task Account_DuplicateEmail_ViolatesUniqueIndex()
    {
        using var database = new TestDatabase();
        await database.MigrateAsync();

        static Account NewAccount(string username) => new()
        {
            Username = username,
            Email = "dup@example.com",
            PasswordHash = "hash",
            CreatedAt = TestData.Now
        };

        await using (var db = database.CreateContext())
        {
            db.Accounts.Add(NewAccount("first"));
            await db.SaveChangesAsync();
        }

        await using (var db = database.CreateContext())
        {
            db.Accounts.Add(NewAccount("second"));
            var save = () => db.SaveChangesAsync();

            await save.Should().ThrowAsync<DbUpdateException>();
        }
    }
}
