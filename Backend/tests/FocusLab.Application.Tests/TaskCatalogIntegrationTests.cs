using FluentAssertions;
using FocusLab.Application.Dto;
using FocusLab.Application.Services;
using FocusLab.Infrastructure;
using FocusLab.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FocusLab.Application.Tests;

public sealed class TaskCatalogIntegrationTests
{
    [Fact]
    public async Task DatabaseMigrationsAndTaskCatalog_PersistProjectsTagsAndChecklist()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;
        var factory = new TestDbContextFactory(options);

        await using (var db = await factory.CreateDbContextAsync())
            await db.Database.MigrateAsync();

        var catalog = new CatalogService(factory);
        var project = await catalog.SaveProjectAsync(new ProjectDto(0, "Integration project", "#10B981"));
        var tag = await catalog.SaveTagAsync(new TagDto(0, "Integration tag", "#A855F7"));
        var tasks = new TaskService(new TaskRepository(factory), new FakeClock());
        var saved = await tasks.CreateAsync(new TaskDto
        {
            Title = "Integration task",
            Project = project.Name,
            TagIds = [tag.Id],
            Subtasks = [new TaskSubtaskDto("subtask-1", "Run checks", false)]
        });

        var loaded = (await tasks.GetAllAsync()).Single(task => task.Id == saved.Id);
        loaded.Project.Should().Be(project.Name);
        loaded.TagIds.Should().ContainSingle().Which.Should().Be(tag.Id);
        loaded.Subtasks.Should().ContainSingle().Which.Title.Should().Be("Run checks");

        var renamed = await catalog.SaveProjectAsync(project with { Name = "Renamed project" });
        (await tasks.GetAllAsync()).Single(task => task.Id == saved.Id).Project.Should().Be(renamed.Name);
        await catalog.DeleteProjectAsync(renamed.Id);
        await catalog.DeleteTagAsync(tag.Id);

        var afterDelete = (await tasks.GetAllAsync()).Single(task => task.Id == saved.Id);
        afterDelete.Project.Should().BeNull();
        afterDelete.TagIds.Should().BeEmpty();
        afterDelete.Subtasks.Should().ContainSingle().Which.Title.Should().Be("Run checks");
    }

    private sealed class TestDbContextFactory(DbContextOptions<AppDbContext> options)
        : IDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext() => new(options);
        public Task<AppDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(CreateDbContext());
    }
}
