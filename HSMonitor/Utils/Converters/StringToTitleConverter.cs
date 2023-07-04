using System;
using System.Globalization;
using System.Windows.Data;
using HSMonitor.ViewModels.Settings;
using MaterialDesignThemes.Wpf;

namespace HSMonitor.Utils.Converters;

[ValueConversion(typeof(string), typeof(string))]
public class StringToTitleConverter : IValueConverter
{
    public static StringToTitleConverter Instance { get; } = new();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not string str) throw new InvalidOperationException("Entered object is not string");
        return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(str);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}