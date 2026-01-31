using System;
using System.Globalization;
using System.Reflection;
using System.Windows.Data;
using HSMonitor.Properties;

namespace HSMonitor.Utils.Converters;

/// <summary>
/// Возвращает локализованное имя вкладки настроек.
///
/// Стратегия:
/// 1) Если у VM есть строковое свойство "LocalizationKey" / "ResourceKey" / "TitleKey" — берём его как ключ в Resources.
/// 2) Иначе пробуем ключ по соглашению: "SettingsTab_{TypeName}" (например SettingsTab_GeneralSettingsTabViewModel).
/// 3) Иначе возвращаем "Name"/"Title" (как fallback).
/// </summary>
public sealed class SettingsTabsToLocalizedNameConverter : IValueConverter
{
    public static SettingsTabsToLocalizedNameConverter Instance { get; } = new();

    private SettingsTabsToLocalizedNameConverter() { }

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is null)
            return string.Empty;

        // 1) Явный ключ из VM (если есть)
        var explicitKey =
            GetStringProperty(value, "Name") ??
            GetStringProperty(value, "ResourceKey") ??
            GetStringProperty(value, "TitleKey");

        if (!string.IsNullOrWhiteSpace(explicitKey))
        {
            var str = Resources.ResourceManager.GetString(explicitKey, CultureInfo.CurrentUICulture);
            if (!string.IsNullOrWhiteSpace(str))
                return str!;
        }

        // 2) Ключ по соглашению: SettingsTab_{TypeName}
        var typeName = value.GetType().Name;
        var conventionKey = $"SettingsTab_{typeName}";
        var convention = Resources.ResourceManager.GetString(conventionKey, culture);
        if (!string.IsNullOrWhiteSpace(convention))
            return convention!;

        // 3) Fallback на Name/Title
        var fallback =
            GetStringProperty(value, "Name") ??
            GetStringProperty(value, "Title") ??
            typeName;

        return fallback ?? string.Empty;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        Binding.DoNothing;

    private static string? GetStringProperty(object obj, string propertyName)
    {
        var prop = obj.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
        if (prop is null || prop.PropertyType != typeof(string))
            return null;

        return prop.GetValue(obj) as string;
    }
}