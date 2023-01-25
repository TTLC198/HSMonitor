using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using HSMonitor.Services;
using HSMonitor.Utils;

namespace HSMonitor.ViewModels.Settings;

public class AppearanceSettingsTabViewModel : SettingsTabBaseViewModel
{
    public string? CpuCustomName
    {
        get => IsAutoDetectHardwareEnabled ? HardwareMonitorService.Cpu.Name : SettingsService.Settings.CpuCustomName;
        set => SettingsService.Settings.CpuCustomName = value;
    }
    
    public string? GpuCustomName
    {
        get => IsAutoDetectHardwareEnabled ? HardwareMonitorService.Gpu.Name : SettingsService.Settings.GpuCustomName;
        set => SettingsService.Settings.GpuCustomName = value;
    }

    public string? CpuCustomType
    {
        get => IsAutoDetectHardwareEnabled ? (HardwareMonitorService.Cpu.Type ?? CpuCustomType ?? "Unknown").SplitByCapitalLettersConvention().First() : SettingsService.Settings.CpuCustomType;
        set => SettingsService.Settings.CpuCustomType = value;
    }
    
    public string? GpuCustomType
    {
        get => IsAutoDetectHardwareEnabled ? (HardwareMonitorService.Gpu.Type ?? GpuCustomType ?? "Unknown").SplitByCapitalLettersConvention().First() : SettingsService.Settings.GpuCustomType;
        set => SettingsService.Settings.GpuCustomType = value;
    }
    
    public string? MemoryCustomType
    {
        get => IsAutoDetectHardwareEnabled ? HardwareMonitorService.Memory.Type : SettingsService.Settings.MemoryCustomType;
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
        set
        {
            if (!value)
            {
                SettingsService.Settings.CpuCustomName = HardwareMonitorService.Cpu.Name;
                SettingsService.Settings.GpuCustomName = HardwareMonitorService.Gpu.Name;
                SettingsService.Settings.CpuCustomType = (HardwareMonitorService.Cpu.Type ?? CpuCustomType ?? "Unknown").SplitByCapitalLettersConvention().First();
                SettingsService.Settings.GpuCustomType = (HardwareMonitorService.Gpu.Type ?? GpuCustomType ?? "Unknown").SplitByCapitalLettersConvention().First();
                SettingsService.Settings.MemoryCustomType = HardwareMonitorService.Memory.Type;
            }
            SettingsService.Settings.IsAutoDetectHardwareEnabled = value;
        }
    }

    public AppearanceSettingsTabViewModel(SettingsService settingsService) 
        : base(settingsService, 2, "Appearance")
    {
    }
}