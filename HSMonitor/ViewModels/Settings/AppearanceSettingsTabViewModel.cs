using System.Collections.Generic;
using HSMonitor.Services;
using static System.String;

namespace HSMonitor.ViewModels.Settings;

public class AppearanceSettingsTabViewModel : SettingsTabBaseViewModel
{
    public string? CpuCustomName
    {
        get => IsNullOrWhiteSpace(SettingsService.Settings.CpuCustomName) ? HardwareMonitorService.Cpu.Name : SettingsService.Settings.CpuCustomName;
        set => SettingsService.Settings.CpuCustomName = value;
    }
    
    public string? GpuCustomName
    {
        get => IsNullOrWhiteSpace(SettingsService.Settings.GpuCustomName) ? HardwareMonitorService.Gpu.Name : SettingsService.Settings.GpuCustomName;
        set => SettingsService.Settings.GpuCustomName = value;
    }

    public string? CpuCustomType
    {
        get => SettingsService.Settings.CpuCustomType;
        set => SettingsService.Settings.CpuCustomType = value;
    }
    
    public string? GpuCustomType
    {
        get => SettingsService.Settings.GpuCustomType;
        set => SettingsService.Settings.GpuCustomType = value;
    }
    
    public string? MemoryCustomType
    {
        get => SettingsService.Settings.MemoryCustomType;
        set => SettingsService.Settings.MemoryCustomType = value;
    }

    public IReadOnlyList<string> CpuCustomTypes { get; } = new List<string>()
    {
        "Amd",
        "Intel"
    };
    
    public IReadOnlyList<string> GpuCustomTypes { get; } = new List<string>()
    {
        "Amd",
        "Nvidia",
        "Intel"
    };
    
    public IReadOnlyList<string> MemoryCustomTypes { get; } = new List<string>()
    {
        "Trident",
        "Viper"
    };

    public bool IsAutoDetectHardwareEnabled
    {
        get => SettingsService.Settings.IsAutoDetectHardwareEnabled;
        set => SettingsService.Settings.IsAutoDetectHardwareEnabled = value;
    }

    public AppearanceSettingsTabViewModel(SettingsService settingsService) 
        : base(settingsService, 1, "Appearance")
    {
    }
}