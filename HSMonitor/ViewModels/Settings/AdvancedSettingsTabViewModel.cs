using System.Collections.Generic;
using System.Globalization;
using HSMonitor.Services;
using HSMonitor.Utils;

namespace HSMonitor.ViewModels.Settings;

public class AdvancedSettingsTabViewModel : SettingsTabBaseViewModel
{
    public bool IsAutoUpdateEnabled
    {
        get => SettingsService.Settings.IsAutoUpdateEnabled;
        set => SettingsService.Settings.IsAutoUpdateEnabled = value;
    }
    public bool IsAutoStartEnabled
    {
        get => SettingsService.Settings.IsAutoStartEnabled;
        set => SettingsService.Settings.IsAutoStartEnabled = value;
    }
    public bool IsHiddenAutoStartEnabled
    {
        get => SettingsService.Settings.IsHiddenAutoStartEnabled;
        set => SettingsService.Settings.IsHiddenAutoStartEnabled = value;
    }
    public bool IsRunAsAdministrator
    {
        get => SettingsService.Settings.IsRunAsAdministrator;
        set => SettingsService.Settings.IsRunAsAdministrator = value;
    }
    
    public string? ApplicationCultureInfo
    {
        get => SettingsService.Settings.ApplicationCultureInfo;
        set => SettingsService.Settings.ApplicationCultureInfo = value;
    }
    
    public static IEnumerable<CultureInfo> Languages => LocalizationManager.GetAvailableCultures();

    public AdvancedSettingsTabViewModel(SettingsService settingsService) 
        : base(settingsService, 3, "Advanced")
    {
    }
}