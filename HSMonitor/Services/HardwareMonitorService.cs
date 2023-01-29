using System;
using System.Collections.Generic;
using System.Linq;
using HSMonitor.Models;
using HSMonitor.Utils.Serial;
using LibreHardwareMonitor.Hardware;

namespace HSMonitor.Services;

public class HardwareMonitorService
{
    public static CpuInformation Cpu = new();
    public static GpuInformation Gpu = new();
    public static MemoryInformation Memory = new();

    private readonly SettingsService _settingsService;

    public event EventHandler? HardwareInformationUpdated;

    private static readonly Computer Computer = new()
    {
        IsCpuEnabled = true,
        IsGpuEnabled = true,
        IsMemoryEnabled = true
    };

    public HardwareMonitorService(SettingsService settingsService)
    {
        _settingsService = settingsService;
        
        _settingsService.SettingsSaved += SettingsServiceOnSettingsSaved;
    }

    private void SettingsServiceOnSettingsSaved(object? sender, EventArgs e)
    {
        if (sender is SettingsService service)
            _settingsService.Settings = service.Settings;
    }

    public Message GetHwInfoMessage()
        => new()
        {
            CpuInformation = Cpu,
            GpuInformation = Gpu,
            MemoryInformation = Memory,
            DeviceSettings = new DeviceSettings
            {
                DisplayBrightness = _settingsService.Settings.DeviceDisplayBrightness
            }
        };

    public static IEnumerable<IHardware> GetProcessors() => Computer.Hardware.Where(h =>
        h.HardwareType == HardwareType.Cpu);
    
    public static IEnumerable<IHardware> GetGraphicCards() => Computer.Hardware.Where(h =>
        h.HardwareType is HardwareType.GpuAmd or HardwareType.GpuIntel or HardwareType.GpuNvidia);

    public void HardwareInformationUpdate()
    {
        try
        {
            Computer.Open();
            Computer.Accept(new UpdateVisitor());

            var cpuHardware = Computer.Hardware
                .FirstOrDefault(h =>
                    h.HardwareType is HardwareType.Cpu &&
                    !string.IsNullOrWhiteSpace(_settingsService.Settings.CpuId) &&
                     h.Identifier.ToString().Contains(_settingsService.Settings.CpuId)) ?? GetProcessors().First();

            var gpuHardware = Computer.Hardware
                .FirstOrDefault(h =>
                    h.HardwareType is HardwareType.GpuAmd or HardwareType.GpuIntel or HardwareType.GpuNvidia &&
                    !string.IsNullOrWhiteSpace(_settingsService.Settings.GpuId) &&
                     h.Identifier.ToString().Contains(_settingsService.Settings.GpuId)) ?? GetGraphicCards().First();
            
            Cpu = CpuInformationUpdate(cpuHardware);
            Gpu = GpuInformationUpdate(gpuHardware);
            Memory = MemoryInformationUpdate(Computer.Hardware
                .FirstOrDefault(h =>
                    h.HardwareType == HardwareType.Memory)!);

            Cpu.DefaultClock = _settingsService.Settings.DefaultCpuFrequency;
            Gpu.DefaultCoreClock = _settingsService.Settings.DefaultGpuFrequency;
        }
        catch
        {
            // ignored
        }
        finally
        {
            OnHardwareInformationUpdated();
        }
    }

    private void OnHardwareInformationUpdated()
    {
        HardwareInformationUpdated?.Invoke(this, EventArgs.Empty);
    }

    private CpuInformation CpuInformationUpdate(IHardware? cpuHardware)
    {
        try
        {
            if (cpuHardware is null)
                throw new Exception();

            var cpuHardwareSensors = cpuHardware
                .Sensors
                .Where(s => s.SensorType is SensorType.Clock or SensorType.SmallData or SensorType.Load
                    or SensorType.Temperature or SensorType.Power)
                .ToArray();

            if (cpuHardwareSensors is null || cpuHardwareSensors.Length == 0)
                throw new Exception();

            var clockSensor = cpuHardwareSensors
                .FirstOrDefault(s => s.SensorType == SensorType.Clock);
            var loadSensor = cpuHardwareSensors
                .FirstOrDefault(s => s.Name.Contains("Total") && s.SensorType == SensorType.Load);
            var powerSensor = cpuHardwareSensors
                .FirstOrDefault(s => s.SensorType == SensorType.Power);
            var temperatureSensor = cpuHardwareSensors
                .FirstOrDefault(s => s.SensorType == SensorType.Temperature);

            return new CpuInformation()
            {
                Type = _settingsService.Settings.IsAutoDetectHardwareEnabled
                    ? (cpuHardware.GetType().ToString().Split(".").LastOrDefault() ?? "Unknown")
                    : _settingsService.Settings.CpuCustomType ??
                      (cpuHardware.GetType().ToString().Split(".").LastOrDefault() ?? "Unknown"),
                Name = _settingsService.Settings.IsAutoDetectHardwareEnabled
                    ? (cpuHardware.Name ?? "Unknown")
                    : string.IsNullOrWhiteSpace(_settingsService.Settings.CpuCustomName)
                        ? cpuHardware.Name
                        : _settingsService.Settings.CpuCustomName,
                Clock = Convert.ToInt32(clockSensor is not null ? clockSensor.Value ?? 0 : 0),
                Load = Convert.ToInt32(loadSensor is not null ? loadSensor.Value ?? 0 : 0),
                Power = Math.Round(
                    Convert.ToDouble(powerSensor is not null ? powerSensor.Value ?? 0 : 0), 1,
                    MidpointRounding.ToEven),
                Temperature =
                    Convert.ToInt32(temperatureSensor is not null ? temperatureSensor.Value ?? 0 : 0),
            };
        }
        catch
        {
            return new CpuInformation()
            {
                Type = "Unknown type",
                Name = "Unknown CPU",
                Clock = 0,
                Load = 0,
                Power = 0,
                Temperature = 0,
            };
        }
    }

    private GpuInformation GpuInformationUpdate(IHardware? gpuHardware)
    {
        try
        {
            if (gpuHardware is null)
                throw new Exception();

            var gpuHardwareSensors = gpuHardware
                .Sensors
                .Where(s => s.SensorType is SensorType.Clock or SensorType.SmallData or SensorType.Load
                    or SensorType.Temperature or SensorType.Power or SensorType.Fan or SensorType.Control)
                .ToArray();

            if (gpuHardwareSensors is null || gpuHardwareSensors.Length == 0)
                throw new Exception();

            var gpuFans = new List<GpuFan>();

            if (gpuHardwareSensors
                    .Count(s => s.SensorType is SensorType.Fan or SensorType.Control) > 0)
                foreach (var fan in gpuHardwareSensors
                             .Where(s => s.SensorType is SensorType.Fan or SensorType.Control)
                             .OrderBy(s => s.Name))
                {
                    switch (fan.SensorType)
                    {
                        case SensorType.Fan when gpuFans.Any(gf => gf.Name == fan.Name):
                            gpuFans.First(gf => gf.Name == fan.Name).Rpm = Convert.ToInt32(fan.Value);
                            break;
                        case SensorType.Fan:
                            gpuFans.Add(new GpuFan()
                            {
                                Name = fan.Name,
                                Rpm = Convert.ToInt32(fan.Value)
                            });
                            break;
                        case SensorType.Control when gpuFans.Any(gf => gf.Name == fan.Name):
                            gpuFans.First(gf => gf.Name == fan.Name).Load = Convert.ToInt32(fan.Value);
                            break;
                        case SensorType.Control:
                            gpuFans.Add(new GpuFan()
                            {
                                Name = fan.Name,
                                Rpm = Convert.ToInt32(fan.Value)
                            });
                            break;
                    }
                }

            var coreClockSensor = gpuHardwareSensors
                .FirstOrDefault(s => s.Name.Contains("Core") && s.SensorType == SensorType.Clock);
            var coreLoadSensor = gpuHardwareSensors
                .FirstOrDefault(s => s.Name.Contains("Core") && s.SensorType == SensorType.Load);
            var coreTemperatureSensor = gpuHardwareSensors
                .FirstOrDefault(s => s.Name.Contains("Core") && s.SensorType == SensorType.Temperature);
            var powerSensor = gpuHardwareSensors
                .FirstOrDefault(s => s.Name.Contains("Package") && s.SensorType == SensorType.Power);
            var vRamClockSensor = gpuHardwareSensors
                .FirstOrDefault(s => s.Name.Contains("Memory") && s.SensorType == SensorType.Clock);
            var vRamMemoryTotalSensor = gpuHardwareSensors
                .FirstOrDefault(s => s.Name.Contains("Memory Total") && s.SensorType == SensorType.SmallData);
            var vRamMemoryUsed = gpuHardwareSensors
                .FirstOrDefault(s => s.Name.Contains("Memory Used") && s.SensorType == SensorType.SmallData);


            return new GpuInformation()
            {
                Type = _settingsService.Settings.IsAutoDetectHardwareEnabled
                    ? (gpuHardware.GetType().ToString().Split(".").LastOrDefault() ?? "Unknown")
                    : _settingsService.Settings.GpuCustomType
                      ?? (gpuHardware.GetType().ToString().Split(".").LastOrDefault() ?? "Unknown"),
                Name = _settingsService.Settings.IsAutoDetectHardwareEnabled
                    ? (gpuHardware.Name ?? "Unknown")
                    : string.IsNullOrWhiteSpace(_settingsService.Settings.GpuCustomName)
                        ? gpuHardware.Name
                        : _settingsService.Settings.GpuCustomName,
                CoreClock = 
                    Convert.ToInt32(coreClockSensor is not null ? coreClockSensor.Value ?? 0 : 0),
                CoreLoad = 
                    Convert.ToInt32(coreLoadSensor is not null ? coreLoadSensor.Value ?? 0 : 0),
                CoreTemperature =
                    Convert.ToInt32(coreTemperatureSensor is not null ? coreTemperatureSensor.Value ?? 0 : 0),
                Power = (double) Math.Round(
                    (decimal) (powerSensor is not null ? powerSensor.Value ?? 0 : 0), 1,
                    MidpointRounding.AwayFromZero),
                VRamClock = 
                    Convert.ToInt32(vRamClockSensor is not null ? vRamClockSensor.Value ?? 0 : 0),
                VRamMemoryTotal =
                    Convert.ToInt32(vRamMemoryTotalSensor is not null ? vRamMemoryTotalSensor.Value ?? 0 : 0),
                VRamMemoryUsed = 
                    Convert.ToInt32(vRamMemoryUsed is not null ? vRamMemoryUsed.Value ?? 0 : 0),
                GpuFans = gpuFans
            };
        }
        catch
        {
            return new GpuInformation()
            {
                Type = "Unknown type",
                Name = "Unknown GPU",
                CoreClock = 0,
                CoreLoad = 0,
                CoreTemperature = 0,
                Power = 0,
                VRamClock = 0,
                VRamMemoryTotal = 0,
                VRamMemoryUsed = 0,
                GpuFans = new List<GpuFan>()
            };
        }
    }

    private MemoryInformation MemoryInformationUpdate(IHardware? memoryHardware)
    {
        try
        {
            if (memoryHardware is null)
                throw new Exception();

            var memoryHardwareSensors = memoryHardware
                .Sensors
                .Where(s => s.SensorType is SensorType.Load or SensorType.Data)
                .ToArray();

            if (memoryHardwareSensors is null || memoryHardwareSensors.Length == 0)
                throw new Exception();

            return new MemoryInformation()
            {
                Type = _settingsService.Settings.IsAutoDetectHardwareEnabled
                    ? "Default"
                    : _settingsService.Settings.MemoryCustomType
                      ?? "Default",
                Load = (int) Math.Round(
                    (decimal) (memoryHardwareSensors.First(s => s.Name == "Memory" && s.SensorType == SensorType.Load)
                        .Value ?? 0), 0, MidpointRounding.AwayFromZero),
                Available = (double) Math.Round(
                    (decimal) (memoryHardwareSensors
                        .First(s => s.Name.Contains("Memory Available") && s.SensorType == SensorType.Data).Value ?? 0),
                    1, MidpointRounding.AwayFromZero),
                Used = (double) Math.Round(
                    (decimal) (memoryHardwareSensors
                        .First(s => s.Name.Contains("Memory Used") && s.SensorType == SensorType.Data).Value ?? 0), 1,
                    MidpointRounding.AwayFromZero),
            };
        }
        catch
        {
            return new MemoryInformation()
            {
                Type = "Default",
                Load = 0,
                Available = 0,
                Used = 0,
            };
        }
    }
}