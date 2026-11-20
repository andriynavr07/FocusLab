using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace FocusLab.Infrastructure.Tests;

/// <summary>#72: міграції на БД, де вже є дані.</summary>
public class MigrationsTests
{
    private const string AddProjects = "20261113004500_AddProjects";

    [Fact]
    public async Task Migrate_FromAddProjects_ExistingTasksGetEmptyChecklist()
    {
        using var database = new TestDatabase();

        // 1) схема на момент AddProjects (ще без checklist_json) + «стара» задача
        await using (var db = database.CreateContext())
        {
            await db.GetService<IMigrator>().MigrateAsync(AddProjects);

            (await db.Database.GetAppliedMigrationsAsync()).Should().NotContain(m => m.EndsWith("_AddTaskChecklist"));

            await db.Database.ExecuteSqlRawAsync(
                "INSERT INTO tasks (account_id, title, priority, status, created_at) " +
                "VALUES (1, 'Legacy task', 'Medium', 'Todo', '2026-11-01 10:00:00')");
        }

        // 2) накочуємо решту міграцій
        await using (var db = database.CreateContext())
        {
            await db.Database.MigrateAsync();
            (await db.Database.GetPendingMigrationsAsync()).Should().BeEmpty();
        }

        // 3) стара задача отримала порожній чекліст
        await using (var db = database.CreateContext())
        {
            var task = await db.Tasks.AsNoTracking().SingleAsync(t => t.Title == "Legacy task");
            task.ChecklistJson.Should().Be("[]");
        }
    }
}
