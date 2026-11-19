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
            UiPreferences.Instance.Initialize(AppPaths.Root);

            _host = Host.CreateDefaultBuilder()
                .ConfigureServices(services =>
                {
                    services.AddInfrastructure();

                    // Application
                    services.AddSingleton<IAuthService, AuthService>();
                    services.AddSingleton<ITaskService, TaskService>();
                    services.AddSingleton<IFocusService, FocusService>();
                    services.AddSingleton<IActivityService, ActivityService>();
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
                $"{UiPreferences.Instance.Translate("Не вдалося запустити застосунок")}:\n{ex.Message}",
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

    /// <summary>Користувач обирає, чи записувати незавершену робочу сесію перед закриттям.</summary>
    private async void OnMainWindowClosing(object? sender, CancelEventArgs e)
    {
        if (_closeConfirmed || _host is null)
            return;

        var timer = _host.Services.GetRequiredService<IFocusTimerService>();
        if (!timer.HasActiveSession && timer.PendingUnsavedSessions == 0)
            return;

        e.Cancel = true;
        if (!timer.HasActiveSession)
        {
            var retry = MessageBox.Show(
                UiPreferences.Instance.Translate("Попередню фокус-сесію не вдалося записати.\n\nТак — повторити збереження.\nНі — закрити без збереження цієї сесії.\nСкасувати — продовжити роботу."),
                "FocusLab",
                MessageBoxButton.YesNoCancel,
                MessageBoxImage.Warning);
            if (retry == MessageBoxResult.Cancel)
                return;

            if (retry == MessageBoxResult.Yes)
            {
                await timer.RetryPendingAsync();
                if (timer.PendingUnsavedSessions > 0)
                {
                    MessageBox.Show(
                        UiPreferences.Instance.Translate("Не вдалося зберегти сесію. Вона залишиться в пам'яті, поки застосунок відкритий."),
                        "FocusLab",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }
            }

            _closeConfirmed = true;
            (sender as Window)?.Close();
            return;
        }

        var answer = MessageBox.Show(
            UiPreferences.Instance.Translate("Фокус-сесія ще триває.\n\nТак — закрити й зберегти її як перервану.\nНі — закрити без збереження незавершеної сесії.\nСкасувати — продовжити роботу."),
            "FocusLab",
            MessageBoxButton.YesNoCancel,
            MessageBoxImage.Warning);
        if (answer == MessageBoxResult.Cancel)
            return;

        try
        {
            await timer.StopAsync(saveInterruptedSession: answer == MessageBoxResult.Yes);
            if (answer == MessageBoxResult.Yes && timer.PendingUnsavedSessions > 0)
            {
                MessageBox.Show(
                    UiPreferences.Instance.Translate("Не вдалося записати перервану сесію. Спробуйте закрити ще раз, щоб повторити збереження."),
                    "FocusLab",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }
        }
        catch (Exception ex)
        {
            _host.Services.GetRequiredService<ILogger<App>>()
                .LogWarning(ex, "Не вдалося зберегти перервану сесію при закритті");
            MessageBox.Show(
                UiPreferences.Instance.Translate("Не вдалося завершити фокус-сесію. Застосунок залишився відкритим."),
                "FocusLab",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            return;
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
