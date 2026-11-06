using System.Windows;

namespace Frontend.Controls;

public static class ControlState
{
    public static readonly DependencyProperty IsInvalidProperty =
        DependencyProperty.RegisterAttached(
            "IsInvalid",
            typeof(bool),
            typeof(ControlState),
            new FrameworkPropertyMetadata(false));

    public static bool GetIsInvalid(DependencyObject element)
        => (bool)element.GetValue(IsInvalidProperty);

    public static void SetIsInvalid(DependencyObject element, bool value)
        => element.SetValue(IsInvalidProperty, value);
}
