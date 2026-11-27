using FluentAssertions;
using FocusLab.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FocusLab.Infrastructure.Tests;

public class AppDbContextTests
{
    [Fact]
    public async Task DeleteTask_CascadesTaskTagsAndFocusSessions()
    {
        using var database = new TestDatabase();
        await database.MigrateAsync();

        await using (var db = database.CreateContext())
        {
            var task = TestData.NewTask("Задача з тегом і сесією");
            task.TaskTags.Add(new TaskTag
            {
                Tag = new Tag { AccountId = Account.LocalAccountId, Name = "Urgent", Color = "#FF0000" }
            });
            task.FocusSessions.Add(new FocusSession
            {
                StartDateTime = TestData.Now,
                EstimatedMinutes = 25,
                DurationMinutes = 25,
                IsCompleted = true
            });
            db.Tasks.Add(task);
            await db.SaveChangesAsync();
        }

        await using (var db = database.CreateContext())
        {
            await db.Tasks.ExecuteDeleteAsync();
        }

        await using (var db = database.CreateContext())
        {
            (await db.TaskTags.CountAsync()).Should().Be(0);
            (await db.FocusSessions.CountAsync()).Should().Be(0);
            (await db.Tags.CountAsync(tag => tag.Name == "Urgent")).Should().Be(1);
        }
    }
}
