using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace FocusLab.Infrastructure.Persistence;

/// <summary>Використовується лише інструментами `dotnet ef` (створення міграцій).</summary>
public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite("Data Source=focuslab.design.db")
            .Options;

        return new AppDbContext(options);
    }
}
