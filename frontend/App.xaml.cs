using FocusLab.Application.Abstractions;
using FocusLab.Application.Services;
using FocusLab.Infrastructure;
using FocusLab.Infrastructure.Persistence;
using FocusLab.Infrastructure.SystemServices;
using frontend.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Windows;

namespace frontend;

public partial class App : System.Windows.Application
{
    private IHost? _host;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        AppPaths.EnsureCreated();
        try
        {
            _host = Host.CreateDefaultBuilder()
                .ConfigureServices(s =>
                {
                    s.AddInfrastructure();
                    s.AddSingleton<ITaskService, TaskService>();
                    s.AddSingleton<IAuthService, AuthService>();

                    s.AddSingleton<MainViewModel>();
                    s.AddTransient<RegisterViewModel>();
                    s.AddTransient<HomeViewModel>();

                    s.AddSingleton<MainWindow>();
                })
                .Build();

            await _host.StartAsync();

            var factory = _host.Services.GetRequiredService<IDbContextFactory<AppDbContext>>();
            await using (var db = await factory.CreateDbContextAsync())
                await db.Database.MigrateAsync();

            _host.Services.GetRequiredService<MainWindow>().Show();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.ToString(), "Помилка запуску");
            Shutdown();
        }
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        if (_host != null)
        {
            await _host.StopAsync();
            _host.Dispose();
        }
        base.OnExit(e);
    }
}