using HSMonitor.Services;

namespace HSMonitor.ViewModels.Settings;

public class AppearanceSettingsTabViewModel : SettingsTabBaseViewModel
{
    public string? CpuCustomName
    {
        get => SettingsService.Settings.CpuCustomName ?? HardwareMonitorService.Cpu.Name;
        set => SettingsService.Settings.CpuCustomName = value;
    }
    
    public string? GpuCustomName
    {
        get => SettingsService.Settings.GpuCustomName ?? HardwareMonitorService.Gpu.Name;
        set => SettingsService.Settings.GpuCustomName = value;
    }

    public bool IsAutodetectHardwareEnabled
    {
        get => SettingsService.Settings.IsAutodetectHardwareEnabled;
        set => SettingsService.Settings.IsAutodetectHardwareEnabled = value;
    }

    public AppearanceSettingsTabViewModel(SettingsService settingsService) 
        : base(settingsService, 1, "Appearance")
    {
    }
}