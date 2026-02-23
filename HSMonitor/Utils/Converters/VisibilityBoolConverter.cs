using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace HSMonitor.Utils.Converters;

[ValueConversion(typeof(bool), typeof(Visibility))]
public class VisibilityBoolConverter : IValueConverter
{
    public static VisibilityBoolConverter Instance { get; } = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var b = value is true;
        if (parameter is string s && s.Equals("invert", StringComparison.OrdinalIgnoreCase))
            b = !b;
        return b 
            ? Visibility.Visible 
            : Visibility.Collapsed;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is Visibility v)
        {
            var b = v == Visibility.Visible;
            if (parameter is string s && s.Equals("invert", StringComparison.OrdinalIgnoreCase))
                b = !b;
            return b;
        }
        return false;
    }
}