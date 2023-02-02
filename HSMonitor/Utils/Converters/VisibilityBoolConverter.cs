using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace HSMonitor.Utils.Converters;

[ValueConversion(typeof(bool), typeof(Visibility))]
public class VisibilityBoolConverter : IValueConverter
{
    public static VisibilityBoolConverter Instance { get; } = new();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is true ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is Visibility.Visible;
}