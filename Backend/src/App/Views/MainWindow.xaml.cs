using System.Windows;
using FocusLab.App.ViewModels;

namespace FocusLab.App.Views;

public partial class MainWindow : Window
{
    private readonly BoardViewModel _board;

    public MainWindow(BoardViewModel board)
    {
        InitializeComponent();

        _board = board;
        DataContext = board;

        Loaded += async (_, _) => await _board.LoadAsync();
    }
}
