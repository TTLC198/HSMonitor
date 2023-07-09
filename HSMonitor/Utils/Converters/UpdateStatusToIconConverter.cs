using System;
using System.Globalization;
using System.Windows.Data;
using MaterialDesignThemes.Wpf;
using NetSparkleUpdater.Enums;

namespace HSMonitor.Utils.Converters;

[ValueConversion(typeof(UpdateStatus), typeof(PackIconKind))]
public class UpdateStatusToIconConverter : IValueConverter
{
    public static UpdateStatusToIconConverter Instance { get; } = new();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => value switch
    {
#pragma warning disable CA1416
        UpdateStatus.UpdateAvailable => PackIconKind.CloudDownloadOutline,
        UpdateStatus.UpdateNotAvailable => PackIconKind.CloudCheckVariantOutline,
        UpdateStatus.UserSkipped => PackIconKind.CloudCancelOutline,
        _ => PackIconKind.CloudAlertOutline
#pragma warning restore CA1416
    };

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}