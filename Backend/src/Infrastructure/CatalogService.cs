using FocusLab.Application.Abstractions;
using FocusLab.Application.Dto;
using FocusLab.Domain.Entities;
using FocusLab.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FocusLab.Infrastructure;

public sealed class CatalogService(IDbContextFactory<AppDbContext> factory) : ICatalogService
{
    public async Task<IReadOnlyList<ProjectDto>> GetProjectsAsync(CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        return await db.Projects.AsNoTracking()
            .Where(project => project.AccountId == Account.LocalAccountId)
            .OrderBy(project => project.Name)
            .Select(project => new ProjectDto(project.Id, project.Name, project.Color))
            .ToListAsync(ct);
    }

    public async Task<ProjectDto> SaveProjectAsync(ProjectDto project, CancellationToken ct = default)
    {
        var name = ValidateName(project.Name);
        var color = ValidateColor(project.Color);
        await using var db = await factory.CreateDbContextAsync(ct);
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        var duplicate = await db.Projects.AnyAsync(
            item => item.AccountId == Account.LocalAccountId && item.Name == name && item.Id != project.Id, ct);
        if (duplicate)
            throw new ArgumentException("Проєкт із такою назвою вже існує.", nameof(project));

        Project entity;
        if (project.Id == 0)
        {
            entity = new Project { AccountId = Account.LocalAccountId };
            db.Projects.Add(entity);
        }
        else
        {
            entity = await db.Projects.FirstOrDefaultAsync(
                item => item.Id == project.Id && item.AccountId == Account.LocalAccountId, ct)
                ?? throw new KeyNotFoundException("Проєкт не знайдено.");
            var oldName = entity.Name;
            if (!string.Equals(oldName, name, StringComparison.Ordinal))
                await db.Tasks.Where(task => task.AccountId == Account.LocalAccountId && task.Project == oldName)
                    .ExecuteUpdateAsync(setters => setters.SetProperty(task => task.Project, name), ct);
        }

        entity.Name = name;
        entity.Color = color;
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return new ProjectDto(entity.Id, entity.Name, entity.Color);
    }

    public async Task DeleteProjectAsync(int id, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        var project = await db.Projects.FirstOrDefaultAsync(
            item => item.Id == id && item.AccountId == Account.LocalAccountId, ct)
            ?? throw new KeyNotFoundException("Проєкт не знайдено.");
        await db.Tasks.Where(task => task.AccountId == Account.LocalAccountId && task.Project == project.Name)
            .ExecuteUpdateAsync(setters => setters.SetProperty(task => task.Project, (string?)null), ct);
        db.Projects.Remove(project);
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
    }

    public async Task<IReadOnlyList<TagDto>> GetTagsAsync(CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        return await db.Tags.AsNoTracking()
            .Where(tag => tag.AccountId == Account.LocalAccountId)
            .OrderBy(tag => tag.Name)
            .Select(tag => new TagDto(tag.Id, tag.Name, tag.Color ?? "#3B6EF5"))
            .ToListAsync(ct);
    }

    public async Task<TagDto> SaveTagAsync(TagDto tag, CancellationToken ct = default)
    {
        var name = ValidateName(tag.Name);
        var color = ValidateColor(tag.Color);
        await using var db = await factory.CreateDbContextAsync(ct);
        var duplicate = await db.Tags.AnyAsync(
            item => item.AccountId == Account.LocalAccountId && item.Name == name && item.Id != tag.Id, ct);
        if (duplicate)
            throw new ArgumentException("Тег із такою назвою вже існує.", nameof(tag));

        Tag entity;
        if (tag.Id == 0)
        {
            entity = new Tag { AccountId = Account.LocalAccountId };
            db.Tags.Add(entity);
        }
        else
        {
            entity = await db.Tags.FirstOrDefaultAsync(
                item => item.Id == tag.Id && item.AccountId == Account.LocalAccountId, ct)
                ?? throw new KeyNotFoundException("Тег не знайдено.");
        }

        entity.Name = name;
        entity.Color = color;
        await db.SaveChangesAsync(ct);
        return new TagDto(entity.Id, entity.Name, entity.Color);
    }

    public async Task DeleteTagAsync(int id, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        var tag = await db.Tags.FirstOrDefaultAsync(
            item => item.Id == id && item.AccountId == Account.LocalAccountId, ct)
            ?? throw new KeyNotFoundException("Тег не знайдено.");
        db.Tags.Remove(tag);
        await db.SaveChangesAsync(ct);
    }

    private static string ValidateName(string? name)
    {
        var trimmed = name?.Trim() ?? string.Empty;
        if (trimmed.Length is < 1 or > 100)
            throw new ArgumentException("Назва має містити від 1 до 100 символів.", nameof(name));
        return trimmed;
    }

    private static string ValidateColor(string? color)
    {
        var value = color?.Trim() ?? string.Empty;
        if (value.Length != 7 || value[0] != '#' || !value[1..].All(Uri.IsHexDigit))
            throw new ArgumentException("Виберіть коректний колір у форматі #RRGGBB.", nameof(color));
        return value.ToUpperInvariant();
    }
}
