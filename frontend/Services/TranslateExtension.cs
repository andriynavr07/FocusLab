using System.Globalization;
using System.Windows.Data;
using System.Windows.Markup;

namespace Frontend.Services;

[MarkupExtensionReturnType(typeof(object))]
public sealed class TranslateExtension : MarkupExtension
{
    public string Text { get; set; } = string.Empty;

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        var binding = new Binding(nameof(UiPreferences.Language))
        {
            Source = UiPreferences.Instance,
            Mode = BindingMode.OneWay,
            Converter = TranslateConverter.Instance,
            ConverterParameter = Text
        };
        return binding.ProvideValue(serviceProvider);
    }

    private sealed class TranslateConverter : IValueConverter
    {
        public static TranslateConverter Instance { get; } = new();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => UiPreferences.Instance.Translate(parameter as string ?? string.Empty);

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => Binding.DoNothing;
    }
}
