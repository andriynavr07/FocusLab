using FocusLab.Application.Abstractions;
using FocusLab.Infrastructure.Persistence;
using FocusLab.Infrastructure.SystemServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FocusLab.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddDbContextFactory<AppDbContext>(options => options.UseSqlite(AppPaths.ConnectionString));
        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<ITaskRepository, TaskRepository>();
        services.AddSingleton<IFocusSessionRepository, FocusSessionRepository>();
        services.AddSingleton<IFocusSettingsProvider>(_ => new JsonFocusSettingsProvider(AppPaths.SettingsFile));
        services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
        services.AddSingleton<ICurrentAccount, CurrentAccount>();
        services.AddSingleton<IAccountRepository, AccountRepository>();
        return services;
    }
}
