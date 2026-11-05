using System.Windows;
using Frontend.ViewModels;

namespace Frontend;

public partial class MainWindow : Window
{
    public MainWindow(MainViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
        Loaded += (_, _) => vm.Start();
    }
}