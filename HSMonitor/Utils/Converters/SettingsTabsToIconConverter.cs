using System;
using System.Globalization;
using System.Windows.Data;
using HSMonitor.ViewModels.Settings;
using MaterialDesignThemes.Wpf;

namespace HSMonitor.Utils.Converters;

[ValueConversion(typeof(ISettingsTabViewModel), typeof(PackIconKind))]
public class SettingsTabsToIconConverter : IValueConverter
{
    public static SettingsTabsToIconConverter Instance { get; } = new();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => value switch
    {
#pragma warning disable CA1416
        ConnectionSettingsTabViewModel => PackIconKind.SerialPort,
        HardwareSettingsTabViewModel => PackIconKind.DesktopTowerMonitor,
        AppearanceSettingsTabViewModel => PackIconKind.Brush,
        AdvancedSettingsTabViewModel => PackIconKind.CheckboxesMarked,
        UpdateSettingsTabViewModel => PackIconKind.CloudOutline,
        _ => PackIconKind.QuestionMark
#pragma warning restore CA1416
    };

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}