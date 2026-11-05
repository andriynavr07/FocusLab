using Frontend.Services;
using Frontend.ViewModels;
using System.Windows;

namespace Frontend;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var window = new MainWindow(new MainViewModel(new LocalAuthSession()));
        MainWindow = window;
        window.Show();
    }
}
