using System.Windows;
using frontend.Services;
using frontend.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

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
                .ConfigureServices(services =>
                {
                    services.AddSingleton<frontend.Services.IAuthService, frontend.Services.AuthService>();
                    services.AddSingleton<MainViewModel>();
                    services.AddTransient<RegisterViewModel>();
                    services.AddTransient<HomeViewModel>();
                    services.AddSingleton<MainWindow>();
                })
                .Build();

            await _host.StartAsync();
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
        if (_host is not null)
        {
            await _host.StopAsync();
            _host.Dispose();
        }

        base.OnExit(e);
    }
}