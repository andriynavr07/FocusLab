using System.IO;
using System.Windows;
using System.Windows.Threading;
using FocusLab.App.ViewModels;
using FocusLab.Application.Abstractions;
using FocusLab.Application.Services;
using FocusLab.Infrastructure;
using FocusLab.Infrastructure.Persistence;
using FocusLab.Infrastructure.SystemServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace FocusLab.App;

public partial class App : System.Windows.Application
{
    private ServiceProvider? _services;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        try
        {
            AppPaths.EnsureCreated();
            ConfigureLogging();
            RegisterGlobalExceptionHandlers();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Не вдалося підготувати каталог даних:\n{ex.Message}", "FocusLab",
                MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(1);
            return;
        }

        try
        {
            _services = BuildServices();

            var factory = _services.GetRequiredService<IDbContextFactory<AppDbContext>>();
            await DatabaseInitializer.InitializeAsync(factory);

            var window = _services.GetRequiredService<Views.MainWindow>();
            MainWindow = window;
            window.Show();

            Log.Information("FocusLab запущено");
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Не вдалося запустити застосунок");
            ShowError(ex);
            Shutdown(1);
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        Log.Information("FocusLab завершено");
        Log.CloseAndFlush();
        _services?.Dispose();
        base.OnExit(e);
    }

    private static ServiceProvider BuildServices()
    {
        var services = new ServiceCollection();

        services.AddInfrastructure();
        services.AddSingleton<ITaskService, TaskService>();

        services.AddSingleton<BoardViewModel>();
        services.AddSingleton<Views.MainWindow>();

        return services.BuildServiceProvider();
    }

    private static void ConfigureLogging()
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.File(
                Path.Combine(AppPaths.LogsDirectory, "focuslab-.log"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 14)
            .CreateLogger();
    }

    private void RegisterGlobalExceptionHandlers()
    {
        DispatcherUnhandledException += (_, args) =>
        {
            Log.Error(args.Exception, "DispatcherUnhandledException");
            ShowError(args.Exception);
            args.Handled = true;
        };

        TaskScheduler.UnobservedTaskException += (_, args) =>
        {
            Log.Error(args.Exception, "TaskScheduler.UnobservedTaskException");
            args.SetObserved();
            Dispatcher.InvokeAsync(() => ShowError(args.Exception));
        };

        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            Log.Fatal(args.ExceptionObject as Exception, "AppDomain.UnhandledException (terminating: {IsTerminating})",
                args.IsTerminating);
            Log.CloseAndFlush();
        };
    }

    private static void ShowError(Exception ex)
    {
        MessageBox.Show(
            $"Сталася помилка: {ex.Message}\n\nДеталі записано в лог:\n{AppPaths.LogsDirectory}",
            "FocusLab",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
    }
}
