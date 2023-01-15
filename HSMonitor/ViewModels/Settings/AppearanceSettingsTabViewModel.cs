using System.Collections.Generic;
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

    public string? CpuCustomType
    {
        get => SettingsService.Settings.CpuCustomType ?? HardwareMonitorService.Cpu.Type;
        set => SettingsService.Settings.CpuCustomType = value;
    }
    
    public string? GpuCustomType
    {
        get => SettingsService.Settings.GpuCustomType ?? HardwareMonitorService.Gpu.Type;
        set => SettingsService.Settings.GpuCustomType = value;
    }
    
    public string? MemoryCustomType
    {
        get => SettingsService.Settings.GpuCustomType ?? HardwareMonitorService.Memory.Type;
        set => SettingsService.Settings.GpuCustomType = value;
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
        "Trident"
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