using FluentAssertions;
using FocusLab.Application.Dto;
using FocusLab.Domain.Entities;
using FocusLab.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FocusLab.Infrastructure.Tests;

public class CatalogServiceTests
{
    private const string Blue = "#3B6EF5";

    private static async Task<(TestDatabase Database, CatalogService Catalog)> CreateAsync()
    {
        var database = new TestDatabase();
        await database.MigrateAsync();
        return (database, new CatalogService(database.Factory));
    }

    private static async Task AddTasksAsync(TestDatabase database, params TaskItem[] tasks)
    {
        await using var db = database.CreateContext();
        db.Tasks.AddRange(tasks);
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task SaveProject_DuplicateName_Throws()
    {
        var (database, catalog) = await CreateAsync();
        using var _ = database;
        await catalog.SaveProjectAsync(new ProjectDto(0, "Alpha", Blue));

        var duplicate = () => catalog.SaveProjectAsync(new ProjectDto(0, "Alpha", "#10B981"));

        await duplicate.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task SaveProject_Rename_UpdatesTasksProject()
    {
        var (database, catalog) = await CreateAsync();
        using var _ = database;
        var project = await catalog.SaveProjectAsync(new ProjectDto(0, "Alpha", Blue));
        await AddTasksAsync(database,
            TestData.NewTask("One", "Alpha"),
            TestData.NewTask("Two", "Alpha"),
            TestData.NewTask("Other", "Gamma"));

        await catalog.SaveProjectAsync(project with { Name = "Beta" });

        await using var db = database.CreateContext();
        var projects = await db.Tasks.AsNoTracking().ToDictionaryAsync(t => t.Title, t => t.Project);
        projects["One"].Should().Be("Beta");
        projects["Two"].Should().Be("Beta");
        projects["Other"].Should().Be("Gamma");
    }

    [Fact]
    public async Task DeleteProject_ClearsTasksProject()
    {
        var (database, catalog) = await CreateAsync();
        using var _ = database;
        var project = await catalog.SaveProjectAsync(new ProjectDto(0, "Alpha", Blue));
        await AddTasksAsync(database, TestData.NewTask("One", "Alpha"), TestData.NewTask("Other", "Gamma"));

        await catalog.DeleteProjectAsync(project.Id);

        await using var db = database.CreateContext();
        var tasks = await db.Tasks.AsNoTracking().ToDictionaryAsync(t => t.Title, t => t.Project);
        tasks["One"].Should().BeNull();
        tasks["Other"].Should().Be("Gamma");
        (await db.Projects.AnyAsync(p => p.Id == project.Id)).Should().BeFalse();
    }

    [Theory]
    [InlineData("red")]
    [InlineData("#12345")]
    [InlineData("#GGGGGG")]
    public async Task SaveProject_InvalidColor_Throws(string color)
    {
        var (database, catalog) = await CreateAsync();
        using var _ = database;

        var save = () => catalog.SaveProjectAsync(new ProjectDto(0, "Alpha", color));

        await save.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task SaveProject_LowercaseColor_StoredUppercase()
    {
        var (database, catalog) = await CreateAsync();
        using var _ = database;

        var saved = await catalog.SaveProjectAsync(new ProjectDto(0, "Alpha", "#3b6ef5"));

        saved.Color.Should().Be("#3B6EF5");
        var stored = (await catalog.GetProjectsAsync()).Single(p => p.Id == saved.Id);
        stored.Color.Should().Be("#3B6EF5");
    }

    [Fact]
    public async Task SaveTag_DuplicateName_Throws()
    {
        var (database, catalog) = await CreateAsync();
        using var _ = database;
        await catalog.SaveTagAsync(new TagDto(0, "Urgent", "#FF0000"));

        var duplicate = () => catalog.SaveTagAsync(new TagDto(0, "Urgent", "#00FF00"));

        await duplicate.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task DeleteTag_RemovesTaskTags()
    {
        var (database, catalog) = await CreateAsync();
        using var _ = database;
        var tag = await catalog.SaveTagAsync(new TagDto(0, "Urgent", "#FF0000"));
        var repository = new TaskRepository(database.Factory);
        var task = TestData.NewTask();
        await repository.AddAsync(task);
        await repository.SetTagsAsync(task.Id, new[] { tag.Id });

        await catalog.DeleteTagAsync(tag.Id);

        await using var db = database.CreateContext();
        (await db.TaskTags.CountAsync()).Should().Be(0);
        (await db.Tasks.CountAsync(t => t.Id == task.Id)).Should().Be(1);
    }
}
