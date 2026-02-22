using HSMonitor.Services;
using HSMonitor.Services.HardwareMonitorService;
using HSMonitor.Utils;

namespace HSMonitor.ViewModels.Settings;

public class AppearanceSettingsTabViewModel(SettingsService settingsService, HardwareMonitorServiceImpl hardwareMonitorServiceImpl) : SettingsTabBaseViewModel(settingsService, 2, "Appearance")
{

    public string? CpuCustomName
    {
        get => IsAutoDetectHardwareEnabled ? hardwareMonitorServiceImpl.Cpu.Name : SettingsService.Settings.CpuCustomName;
        set => SettingsService.Settings.CpuCustomName = value;
    }
    
    public string? GpuCustomName
    {
        get => IsAutoDetectHardwareEnabled ? hardwareMonitorServiceImpl.Gpu.Name : SettingsService.Settings.GpuCustomName;
        set => SettingsService.Settings.GpuCustomName = value;
    }

    public string? CpuCustomType
    {
        get => IsAutoDetectHardwareEnabled ? (hardwareMonitorServiceImpl.Cpu.Type ?? SettingsService.Settings.CpuCustomType ?? "Unknown").SplitByCapitalLettersConvention().First() : SettingsService.Settings.CpuCustomType;
        set => SettingsService.Settings.CpuCustomType = value;
    }
    
    public string? GpuCustomType
    {
        get => IsAutoDetectHardwareEnabled ? (hardwareMonitorServiceImpl.Gpu.Type ?? SettingsService.Settings.GpuCustomType ?? "Unknown").SplitByCapitalLettersConvention().First() : SettingsService.Settings.GpuCustomType;
        set => SettingsService.Settings.GpuCustomType = value;
    }
    
    public string? MemoryCustomType
    {
        get => IsAutoDetectHardwareEnabled ? (hardwareMonitorServiceImpl.Memory.Type ?? SettingsService.Settings.MemoryCustomType ?? "Unknown").SplitByCapitalLettersConvention().First() : SettingsService.Settings.MemoryCustomType;
        set => SettingsService.Settings.MemoryCustomType = value;
    }

    public IReadOnlyList<string> CpuCustomTypes { get; } = new List<string>
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
        "Default",
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
                SettingsService.Settings.CpuCustomName = hardwareMonitorServiceImpl.Cpu.Name;
                SettingsService.Settings.GpuCustomName = hardwareMonitorServiceImpl.Gpu.Name;
                SettingsService.Settings.CpuCustomType = (hardwareMonitorServiceImpl.Cpu.Type ?? CpuCustomType ?? "Unknown").SplitByCapitalLettersConvention().First();
                SettingsService.Settings.GpuCustomType = (hardwareMonitorServiceImpl.Gpu.Type ?? GpuCustomType ?? "Unknown").SplitByCapitalLettersConvention().First();
                SettingsService.Settings.MemoryCustomType = (hardwareMonitorServiceImpl.Memory.Type ?? MemoryCustomType ?? "Unknown").SplitByCapitalLettersConvention().First();;
            }
            SettingsService.Settings.IsAutoDetectHardwareEnabled = value;
        }
    }

    public int CustomNameMaxLength => SettingsService.Settings.IsDeviceBackwardCompatibilityEnabled 
        ? 23 
        : 30;

}