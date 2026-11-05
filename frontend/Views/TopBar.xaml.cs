using System.Windows;
using System.Windows.Controls;

namespace Frontend.Views;

public partial class TopBar : UserControl
{
    public static readonly DependencyProperty TitleProperty =
        DependencyProperty.Register(nameof(Title), typeof(string), typeof(TopBar), new PropertyMetadata(""));

    public static readonly DependencyProperty ShowSearchProperty =
        DependencyProperty.Register(nameof(ShowSearch), typeof(bool), typeof(TopBar), new PropertyMetadata(true));

    public static readonly DependencyProperty ShowAddButtonProperty =
        DependencyProperty.Register(nameof(ShowAddButton), typeof(bool), typeof(TopBar), new PropertyMetadata(true));

    public string Title { get => (string)GetValue(TitleProperty); set => SetValue(TitleProperty, value); }
    public bool ShowSearch { get => (bool)GetValue(ShowSearchProperty); set => SetValue(ShowSearchProperty, value); }
    public bool ShowAddButton { get => (bool)GetValue(ShowAddButtonProperty); set => SetValue(ShowAddButtonProperty, value); }

    public TopBar() => InitializeComponent();
}