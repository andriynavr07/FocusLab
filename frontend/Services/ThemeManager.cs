using System.Windows;

namespace Frontend.Services;

public static class ThemeManager
{
    public static void Apply(bool useLightTheme)
    {
        var resources = Application.Current.Resources.MergedDictionaries;
        var currentTheme = resources.FirstOrDefault(dictionary =>
            dictionary.Source?.OriginalString.EndsWith("Theme.Dark.xaml", StringComparison.OrdinalIgnoreCase) == true
            || dictionary.Source?.OriginalString.EndsWith("Theme.Light.xaml", StringComparison.OrdinalIgnoreCase) == true);

        var theme = new ResourceDictionary
        {
            Source = new Uri(
                useLightTheme ? "Resources/Theme.Light.xaml" : "Resources/Theme.Dark.xaml",
                UriKind.Relative)
        };

        if (currentTheme is null)
            resources.Add(theme);
        else
            resources[resources.IndexOf(currentTheme)] = theme;
    }
}
