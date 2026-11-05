using System.ComponentModel;
using System.Windows;
using FocusLab.Application.Abstractions;
using FocusLab.Application.Focus;
using FocusLab.Application.Services;
using FocusLab.Infrastructure;
using FocusLab.Infrastructure.Persistence;
using FocusLab.Infrastructure.SystemServices;
using Frontend.Services;
using Frontend.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Frontend;

public partial class App : Application
{
    private IHost? _host;
    private bool _closeConfirmed;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Поки йде ініціалізація БД, вікна ще немає — не даємо застосунку закритися.
        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        try
        {
            AppPaths.EnsureCreated();

            _host = Host.CreateDefaultBuilder()
                .ConfigureServices(services =>
                {
                    services.AddInfrastructure();

                    // Application
                    services.AddSingleton<IAuthService, AuthService>();
                    services.AddSingleton<ITaskService, TaskService>();
                    services.AddSingleton<IFocusService, FocusService>();
                    services.AddSingleton<IFocusTimerService, FocusTimerService>();

                    // Реалізації, яким потрібен Windows (живуть у frontend)
                    services.AddSingleton<INotificationService, WindowsNotificationService>();
                    services.AddSingleton<ISoundPlayer, SystemSoundPlayer>();

                    services.AddTransient<MainViewModel>();
                })
                .Build();

            await _host.StartAsync();

            var dbFactory = _host.Services.GetRequiredService<IDbContextFactory<AppDbContext>>();
            await DatabaseInitializer.InitializeAsync(dbFactory);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Не вдалося запустити застосунок:\n{ex.Message}",
                "FocusLab",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown(1);
            return;
        }

        var window = new MainWindow(_host.Services.GetRequiredService<MainViewModel>());
        window.Closing += OnMainWindowClosing;
        MainWindow = window;
        ShutdownMode = ShutdownMode.OnMainWindowClose;
        window.Show();
    }

    /// <summary>Незавершена фокус-сесія при закритті — попередити й зберегти її як перервану (SC-06, A2).</summary>
    private async void OnMainWindowClosing(object? sender, CancelEventArgs e)
    {
        if (_closeConfirmed || _host is null)
            return;

        var timer = _host.Services.GetRequiredService<IFocusTimerService>();
        if (!timer.HasActiveSession)
            return;

        e.Cancel = true;
        var answer = MessageBox.Show(
            "Фокус-сесія ще триває. Завершити її та закрити FocusLab?",
            "FocusLab",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);
        if (answer != MessageBoxResult.Yes)
            return;

        try
        {
            await timer.StopAsync();
        }
        catch (Exception ex)
        {
            _host.Services.GetRequiredService<ILogger<App>>()
                .LogWarning(ex, "Не вдалося зберегти перервану сесію при закритті");
        }

        _closeConfirmed = true;
        (sender as Window)?.Close();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        // Dispose хоста диспозить singleton-и, зокрема сторожовий таймер FocusTimerService.
        _host?.Dispose();
        base.OnExit(e);
    }
}
