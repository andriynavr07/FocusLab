using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace Frontend.Converters;

public sealed class ProgressToDashConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not double progress || parameter is not string dimensions)
            throw new InvalidOperationException("Progress and ellipse dimensions are required.");

        var parts = dimensions.Split(';');
        if (parts.Length != 2
            || !double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var diameter)
            || !double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var thickness)
            || diameter <= thickness
            || thickness <= 0)
        {
            throw new ArgumentException("Ellipse dimensions must contain a valid diameter and stroke thickness.");
        }

        var circumference = Math.PI * (diameter - thickness);
        var clampedProgress = Math.Clamp(progress, 0, 1);
        var dash = circumference * clampedProgress / thickness;
        var gap = circumference * (1 - clampedProgress) / thickness;
        return new DoubleCollection { dash, gap };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
