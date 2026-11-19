using System.Windows;
using System.Windows.Controls;

namespace Frontend.Controls;

/// <summary>Іконки всередині полів вводу (логін / реєстрація) та плейсхолдер для PasswordBox.</summary>
public static class FieldIcons
{
    public static readonly DependencyProperty LeadingProperty =
        DependencyProperty.RegisterAttached("Leading", typeof(string), typeof(FieldIcons), new PropertyMetadata(null));
    public static string? GetLeading(DependencyObject o) => (string?)o.GetValue(LeadingProperty);
    public static void SetLeading(DependencyObject o, string? v) => o.SetValue(LeadingProperty, v);

    public static readonly DependencyProperty TrailingProperty =
        DependencyProperty.RegisterAttached("Trailing", typeof(string), typeof(FieldIcons), new PropertyMetadata(null));
    public static string? GetTrailing(DependencyObject o) => (string?)o.GetValue(TrailingProperty);
    public static void SetTrailing(DependencyObject o, string? v) => o.SetValue(TrailingProperty, v);

    public static readonly DependencyProperty IsEmptyProperty =
        DependencyProperty.RegisterAttached("IsEmpty", typeof(bool), typeof(FieldIcons), new PropertyMetadata(true));
    public static bool GetIsEmpty(DependencyObject o) => (bool)o.GetValue(IsEmptyProperty);
    public static void SetIsEmpty(DependencyObject o, bool v) => o.SetValue(IsEmptyProperty, v);

    /// <summary>Підписується на зміну пароля, щоб показувати плейсхолдер (Tag) у порожньому PasswordBox.</summary>
    public static readonly DependencyProperty TracksPasswordProperty =
        DependencyProperty.RegisterAttached("TracksPassword", typeof(bool), typeof(FieldIcons),
            new PropertyMetadata(false, OnTracksPasswordChanged));
    public static bool GetTracksPassword(DependencyObject o) => (bool)o.GetValue(TracksPasswordProperty);
    public static void SetTracksPassword(DependencyObject o, bool v) => o.SetValue(TracksPasswordProperty, v);

    private static void OnTracksPasswordChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not PasswordBox box) return;
        box.PasswordChanged -= OnPasswordChanged;
        if ((bool)e.NewValue)
        {
            box.PasswordChanged += OnPasswordChanged;
            SetIsEmpty(box, box.Password.Length == 0);
        }
    }

    private static void OnPasswordChanged(object sender, RoutedEventArgs e)
    {
        var box = (PasswordBox)sender;
        SetIsEmpty(box, box.Password.Length == 0);
    }
}
