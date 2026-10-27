using FocusLab.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FocusLab.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<TaskItem> Tasks => Set<TaskItem>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<TaskTag> TaskTags => Set<TaskTag>();
    public DbSet<TaskResource> TaskResources => Set<TaskResource>();
    public DbSet<FocusSession> FocusSessions => Set<FocusSession>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
