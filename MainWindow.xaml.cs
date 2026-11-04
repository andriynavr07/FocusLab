using System.Windows;
using frontend.ViewModels;

namespace frontend;

public partial class MainWindow : Window
{
    public MainWindow(MainViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
        Loaded += async (_, _) => await vm.StartAsync();
    }
}