using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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

    private readonly Computer _computer = new Computer
    {
        IsCpuEnabled = true,
        IsGpuEnabled = true,
        IsMemoryEnabled = true
    };
    
    public HardwareMonitorService(SettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    public Message GetHwInfoMessage()
        => new ()
        {
            CpuInformation = Cpu,
            GpuInformation = Gpu,
            MemoryInformation = Memory,
        };
    
    public void HardwareInformationUpdate()
    {
        try
        {
            _computer.Open();
            _computer.Accept(new UpdateVisitor());

            Cpu = CpuInformationUpdate(_computer);
            Gpu = GpuInformationUpdate(_computer);
            Memory = MemoryInformationUpdate(_computer);
        }
        catch
        {
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

    private CpuInformation CpuInformationUpdate(Computer computer)
    {
        try
        {
            var cpuHardware = computer.Hardware
                .FirstOrDefault(h => h.HardwareType == HardwareType.Cpu);

            if (cpuHardware is null)
                throw new Exception();

            var cpuHardwareSensors = cpuHardware
                .Sensors
                .Where(s => s.SensorType is SensorType.Clock or SensorType.SmallData or SensorType.Load
                    or SensorType.Temperature or SensorType.Power)
                .ToArray();
            
            if (cpuHardwareSensors is null || cpuHardwareSensors.Length == 0)
                throw new Exception();

            return new CpuInformation()
            {
                Type = _settingsService.Settings.IsAutoDetectHardwareEnabled 
                    ? (cpuHardware.GetType().ToString().Split(".").Last() ?? "Unknown") 
                    : _settingsService.Settings.CpuCustomType ?? (cpuHardware.GetType().ToString().Split(".").Last() ?? "Unknown"),
                Name = _settingsService.Settings.IsAutoDetectHardwareEnabled 
                    ? (cpuHardware.Name ?? "Unknown")
                    : string.IsNullOrWhiteSpace(_settingsService.Settings.CpuCustomName) ? cpuHardware.Name : _settingsService.Settings.CpuCustomName,
                Clock = Convert.ToInt32(cpuHardwareSensors.First(s => s.SensorType == SensorType.Clock).Value ?? 0),
                Load = Convert.ToInt32(cpuHardwareSensors
                    .First(s => s.Name.Contains("Total") && s.SensorType == SensorType.Load).Value ?? 0),
                Power = Math.Round(
                    Convert.ToDouble(cpuHardwareSensors.First(s => s.SensorType == SensorType.Power).Value ?? 0), 1,
                    MidpointRounding.ToEven),
                Temperature =
                    Convert.ToInt32(cpuHardwareSensors.First(s => s.SensorType == SensorType.Temperature).Value ?? 0),
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

    private GpuInformation GpuInformationUpdate(Computer computer)
    {
        try
        {
            var gpuHardware = computer.Hardware
                .FirstOrDefault(h => h.HardwareType is HardwareType.GpuAmd or HardwareType.GpuIntel or HardwareType.GpuNvidia);

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

            return new GpuInformation()
            {
                Type = _settingsService.Settings.IsAutoDetectHardwareEnabled 
                    ? (gpuHardware.GetType().ToString().Split(".").Last() ?? "Unknown") 
                    : _settingsService.Settings.GpuCustomType 
                      ?? (gpuHardware.GetType().ToString().Split(".").Last() ?? "Unknown"),
                Name = _settingsService.Settings.IsAutoDetectHardwareEnabled 
                    ? (gpuHardware.Name ?? "Unknown") 
                    : string.IsNullOrWhiteSpace(_settingsService.Settings.GpuCustomName) ? gpuHardware.Name : _settingsService.Settings.GpuCustomName,
                CoreClock = Convert.ToInt32(gpuHardwareSensors
                    .First(s => s.Name.Contains("Core") && s.SensorType == SensorType.Clock).Value ?? 0),
                CoreLoad = Convert.ToInt32(gpuHardwareSensors
                    .First(s => s.Name.Contains("Core") && s.SensorType == SensorType.Load).Value ?? 0),
                CoreTemperature = Convert.ToInt32(gpuHardwareSensors
                    .First(s => s.Name.Contains("Core") && s.SensorType == SensorType.Temperature).Value ?? 0),
                Power = (double) Math.Round(
                    (decimal) (gpuHardwareSensors
                        .First(s => s.Name.Contains("Package") && s.SensorType == SensorType.Power).Value ?? 0), 1,
                    MidpointRounding.AwayFromZero),
                VRamClock = Convert.ToInt32(gpuHardwareSensors
                    .First(s => s.Name.Contains("Memory") && s.SensorType == SensorType.Clock).Value ?? 0),
                VRamMemoryTotal = Convert.ToInt32(gpuHardwareSensors
                    .First(s => s.Name.Contains("Memory Total") && s.SensorType == SensorType.SmallData).Value ?? 0),
                VRamMemoryUsed = Convert.ToInt32(gpuHardwareSensors
                    .First(s => s.Name.Contains("Memory Used") && s.SensorType == SensorType.SmallData).Value ?? 0),
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

    private MemoryInformation MemoryInformationUpdate(Computer computer)
    {
        try
        {
            var memoryHardware = computer.Hardware
                .First(h => h.HardwareType == HardwareType.Memory);

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
                    ? "Trident"
                    : _settingsService.Settings.MemoryCustomType 
                      ?? "Trident",
                Load = (int)Math.Round(
                    (decimal) (memoryHardwareSensors.First(s => s.Name == "Memory" && s.SensorType == SensorType.Load)
                        .Value ?? 0), 0, MidpointRounding.AwayFromZero),
                Available = (double)Math.Round(
                    (decimal) (memoryHardwareSensors
                        .First(s => s.Name.Contains("Memory Available") && s.SensorType == SensorType.Data).Value ?? 0), 1,MidpointRounding.AwayFromZero),
                Used = (double)Math.Round(
                    (decimal) (memoryHardwareSensors
                        .First(s => s.Name.Contains("Memory Used") && s.SensorType == SensorType.Data).Value ?? 0), 1,MidpointRounding.AwayFromZero),
            };
        }
        catch
        {
            return new MemoryInformation()
            {
                Type = "Trident",
                Load = 0,
                Available = 0,
                Used = 0,
            };
        }
    }
}