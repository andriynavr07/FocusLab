using System;
using System.Net.Http;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;

namespace Frontend;

public partial class App : Application
{

    public IServiceProvider ServiceProvider { get; private set; }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var services = new ServiceCollection();

        // 1. Реєструємо HttpClient із базовою адресою твого бекенду
        services.AddHttpClient("BackendClient", client =>
        {
            // УВАГА: Заміни порт 7123 на той, на якому запускається твій FocusLab.App
            client.BaseAddress = new Uri("https://localhost:7123/");
        });
        services.AddTransient<MainWindow>();

        ServiceProvider = services.BuildServiceProvider();
        var mainWindow = ServiceProvider.GetRequiredService<MainWindow>();
        mainWindow.Show();
    }
}