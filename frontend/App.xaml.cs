using System.Windows;
using FocusLab.Application.Abstractions;
using FocusLab.Application.Services;
using FocusLab.Infrastructure;
using FocusLab.Infrastructure.Persistence;
using FocusLab.Infrastructure.SystemServices;
using Frontend.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Frontend;

public partial class App : Application
{
    private ServiceProvider? _services;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);


        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        try
        {
            AppPaths.EnsureCreated();

            var services = new ServiceCollection();
            services.AddInfrastructure();
            services.AddSingleton<IAuthService, AuthService>();
            services.AddSingleton<ITaskService, TaskService>();
            services.AddTransient<MainViewModel>();
            _services = services.BuildServiceProvider();

            var dbFactory = _services.GetRequiredService<IDbContextFactory<AppDbContext>>();
            await DatabaseInitializer.InitializeAsync(dbFactory);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Не вдалося ініціалізувати базу даних:\n{ex.Message}",
                "FocusLab",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown(1);
            return;
        }

        var window = new MainWindow(_services.GetRequiredService<MainViewModel>());
        MainWindow = window;
        ShutdownMode = ShutdownMode.OnMainWindowClose;
        window.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _services?.Dispose();
        base.OnExit(e);
    }
}
