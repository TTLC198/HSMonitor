using HSMonitor.Services;

namespace HSMonitor.ViewModels.Settings;

public class AdvancedSettingsTabViewModel : SettingsTabBaseViewModel
{
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

    public AdvancedSettingsTabViewModel(SettingsService settingsService) 
        : base(settingsService, 2, "Advanced")
    {
    }
}