using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using HSMonitor.Models;
using HSMonitor.Utils;
using HSMonitor.Utils.Logger;
using HSMonitor.Utils.Serial;
using LibreHardwareMonitor.Hardware;
using MaterialDesignThemes.Wpf;

namespace HSMonitor.Services;

public class HardwareMonitorService
{
    public CpuInformation Cpu = new();
    public GpuInformation Gpu = new();
    public MemoryInformation Memory = new();

    private readonly SettingsService _settingsService;
    private readonly ILogger<HardwareMonitorService> _logger;

    public event EventHandler? HardwareInformationUpdated;

    private DispatcherTimer _updateHardwareMonitorTimer = new();

    private static readonly Computer Computer = new()
    {
        IsCpuEnabled = true,
        IsGpuEnabled = true,
        IsMemoryEnabled = true
    };

    public HardwareMonitorService(SettingsService settingsService, ILogger<HardwareMonitorService> logger)
    {
        _settingsService = settingsService;
        _logger = logger;

        _settingsService.SettingsSaved += SettingsServiceOnSettingsSaved;
    }

    public async Task Start()
    {
        _updateHardwareMonitorTimer = new DispatcherTimer(
            priority: DispatcherPriority.Background,
            interval: TimeSpan.FromMilliseconds(_settingsService.Settings.SendInterval == 0
                ? 500
                : _settingsService.Settings.SendInterval),
            callback: HardwareInformationUpdate,
            dispatcher: Dispatcher.FromThread(Thread.CurrentThread) ?? throw new InvalidOperationException("Current thread is null")
        );
        await Task.Run(() =>
        {
            _updateHardwareMonitorTimer.Start();
        });
    }

    private void SettingsServiceOnSettingsSaved(object? sender, EventArgs e)
    {
        if (sender is not SettingsService service) return;
        _settingsService.Settings = service.Settings;
        _updateHardwareMonitorTimer.Interval = TimeSpan.FromMilliseconds(
            _settingsService.Settings.SendInterval == 0 
                ? 500 
                :_settingsService.Settings.SendInterval);
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

    public async void HardwareInformationUpdate(object? sender, EventArgs eventArgs)
    {
        try
        {
            Computer.Open();
            Computer.Accept(new UpdateVisitor());

            var cpuHardware = Computer.Hardware
                .FirstOrDefault(h =>
                    h.HardwareType is HardwareType.Cpu &&
                    !string.IsNullOrWhiteSpace(_settingsService.Settings.CpuId) &&
                    h.Identifier.ToString()!.Contains(_settingsService.Settings.CpuId)) ?? GetProcessors().First();

            var gpuHardware = Computer.Hardware
                .FirstOrDefault(h =>
                    h.HardwareType is HardwareType.GpuAmd or HardwareType.GpuIntel or HardwareType.GpuNvidia &&
                    !string.IsNullOrWhiteSpace(_settingsService.Settings.GpuId) &&
                    h.Identifier.ToString()!.Contains(_settingsService.Settings.GpuId)) ?? GetGraphicCards().First();

            Cpu = CpuInformationUpdate(cpuHardware);
            Gpu = GpuInformationUpdate(gpuHardware);
            Memory = MemoryInformationUpdate(Computer.Hardware
                .FirstOrDefault(h =>
                    h.HardwareType == HardwareType.Memory)!);

            Cpu.DefaultClock = _settingsService.Settings.DefaultCpuFrequency;
            Gpu.DefaultCoreClock = _settingsService.Settings.DefaultGpuFrequency;
        }
        catch (Exception exception)
        {
            _logger.Error(exception);
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
        var cpuInfo = new CpuInformation()
        {
            Type = "Unknown type",
            Name = "Unknown CPU",
            Clock = 0,
            Load = 0,
            Power = 0,
            Temperature = 0,
        };
        
        try
        {
            if (cpuHardware is null)
                return cpuInfo;

            var cpuHardwareSensors = cpuHardware
                .Sensors
                .Where(s => s.SensorType is SensorType.Clock or SensorType.SmallData or SensorType.Load
                    or SensorType.Temperature or SensorType.Power or SensorType.Voltage)
                .ToArray();

            if (cpuHardwareSensors is null or {Length: 0})
                return cpuInfo;
            
            var clockSensor = cpuHardwareSensors
                .FirstOrDefault(s => s.SensorType == SensorType.Clock);
            var loadSensor = cpuHardwareSensors
                .FirstOrDefault(s => s.Name.Contains("Total") && s.SensorType == SensorType.Load);
            var powerSensor = cpuHardwareSensors
                .FirstOrDefault(s => s.SensorType == SensorType.Power);
            var voltageSensor = cpuHardwareSensors
                .FirstOrDefault(s => s.SensorType == SensorType.Voltage && s.Name.Contains("Core") && !s.Name.Contains("VID"));
            var temperatureSensor = cpuHardwareSensors
                .FirstOrDefault(s => s.SensorType == SensorType.Temperature);
            
            cpuInfo.Type = _settingsService.Settings.IsAutoDetectHardwareEnabled
                ? cpuHardware.GetType().ToString().Split(".").LastOrDefault() ?? "Unknown"
                : _settingsService.Settings.CpuCustomType ??
                  (cpuHardware.GetType().ToString().Split(".").LastOrDefault() ?? "Unknown");

            cpuInfo.Name = _settingsService.Settings.IsAutoDetectHardwareEnabled
                ? (cpuHardware.Name ?? "Unknown")
                : string.IsNullOrWhiteSpace(_settingsService.Settings.CpuCustomName)
                    ? cpuHardware.Name
                    : _settingsService.Settings.CpuCustomName;

            if (clockSensor is not null and {Value: not null})
                cpuInfo.Clock = Convert.ToInt32(clockSensor.Value.GetFloatAsCorrectNumber());
            if (loadSensor is not null and {Value: not null})
                cpuInfo.Load = Convert.ToInt32(loadSensor.Value.GetFloatAsCorrectNumber());
            if (powerSensor is not null and {Value: not null})
                cpuInfo.Power = Math.Round(
                    powerSensor.Value.GetFloatAsCorrectNumber(),
                    1,
                    MidpointRounding.ToEven);
            if (voltageSensor is not null and {Value: not null})
                cpuInfo.Voltage = Math.Round(
                    voltageSensor.Value.GetFloatAsCorrectNumber(),
                    2,
                    MidpointRounding.ToEven);
            if (temperatureSensor is not null and {Value: not null})
                cpuInfo.Temperature = Convert.ToInt32(temperatureSensor.Value.GetFloatAsCorrectNumber());

            return cpuInfo;
        }
        catch (Exception exception)
        {
            _logger.Error(exception);
            return cpuInfo;
        }
    }

    private GpuInformation GpuInformationUpdate(IHardware? gpuHardware)
    {
        var gpuInfo = new GpuInformation()
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
        
        try
        {
            if (gpuHardware is null)
                return gpuInfo;

            var gpuHardwareSensors = gpuHardware
                .Sensors
                .Where(s => s.SensorType is SensorType.Clock or SensorType.SmallData or SensorType.Load
                    or SensorType.Temperature or SensorType.Power or SensorType.Fan or SensorType.Control)
                .ToArray();

            if (gpuHardwareSensors is null or {Length: 0})
                return gpuInfo;

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
                            gpuFans.First(gf => gf.Name == fan.Name).Rpm = Convert.ToInt32(fan.Value.GetFloatAsCorrectNumber());
                            break;
                        case SensorType.Fan:
                            gpuFans.Add(new GpuFan()
                            {
                                Name = fan.Name,
                                Rpm = Convert.ToInt32(fan.Value.GetFloatAsCorrectNumber())
                            });
                            break;
                        case SensorType.Control when gpuFans.Any(gf => gf.Name == fan.Name):
                            gpuFans.First(gf => gf.Name == fan.Name).Load = Convert.ToInt32(fan.Value.GetFloatAsCorrectNumber());
                            break;
                        case SensorType.Control:
                            gpuFans.Add(new GpuFan()
                            {
                                Name = fan.Name,
                                Rpm = Convert.ToInt32(fan.Value.GetFloatAsCorrectNumber())
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
            var vRamMemoryUsedSensor = gpuHardwareSensors
                .FirstOrDefault(s => s.Name.Contains("Memory Used") && s.SensorType == SensorType.SmallData);

            gpuInfo.Type = _settingsService.Settings.IsAutoDetectHardwareEnabled
                ? (gpuHardware.GetType().ToString().Split(".").LastOrDefault() ?? "Unknown")
                : _settingsService.Settings.GpuCustomType
                  ?? (gpuHardware.GetType().ToString().Split(".").LastOrDefault() ?? "Unknown");
            
            gpuInfo.Name = _settingsService.Settings.IsAutoDetectHardwareEnabled
                ? (gpuHardware.Name ?? "Unknown")
                : string.IsNullOrWhiteSpace(_settingsService.Settings.GpuCustomName)
                    ? gpuHardware.Name
                    : _settingsService.Settings.GpuCustomName;
            
            if (coreClockSensor is not null and {Value: not null})
                gpuInfo.CoreClock = Convert.ToInt32(coreClockSensor.Value.GetFloatAsCorrectNumber());
            if (coreLoadSensor is not null and {Value: not null})
                gpuInfo.CoreLoad = Convert.ToInt32(coreLoadSensor.Value.GetFloatAsCorrectNumber());
            if (coreTemperatureSensor is not null and {Value: not null})
                gpuInfo.CoreTemperature = Convert.ToInt32(coreTemperatureSensor.Value.GetFloatAsCorrectNumber());
            if (powerSensor is not null and {Value: not null})
                gpuInfo.Power = Convert.ToInt32(powerSensor.Value.GetFloatAsCorrectNumber());
            if (vRamClockSensor is not null and {Value: not null})
                gpuInfo.VRamClock = Convert.ToInt32(vRamClockSensor.Value.GetFloatAsCorrectNumber());
            if (vRamMemoryTotalSensor is not null and {Value: not null})
                gpuInfo.VRamMemoryTotal = Convert.ToInt32(vRamMemoryTotalSensor.Value.GetFloatAsCorrectNumber());
            if (vRamMemoryUsedSensor is not null and {Value: not null})
                gpuInfo.VRamMemoryUsed = Convert.ToInt32(vRamMemoryUsedSensor.Value.GetFloatAsCorrectNumber());
            
            gpuInfo.GpuFans = gpuFans;

            return gpuInfo;
        }
        catch (Exception exception)
        {
            _logger.Error(exception);
            return gpuInfo;
        }
    }

    private MemoryInformation MemoryInformationUpdate(IHardware? memoryHardware)
    {
        var memoryInfo = new MemoryInformation()
        {
            Type = "Default",
            Load = 0,
            Available = 0,
            Used = 0,
        };

        try
        {
            if (memoryHardware is null)
                return memoryInfo;

            var memoryHardwareSensors = memoryHardware
                .Sensors
                .Where(s => s.SensorType is SensorType.Load or SensorType.Data)
                .ToArray();

            if (memoryHardwareSensors is null or {Length: 0})
                return memoryInfo;

            memoryInfo.Type = _settingsService.Settings.IsAutoDetectHardwareEnabled
                ? "Default"
                : _settingsService.Settings.MemoryCustomType
                  ?? "Default";
            memoryInfo.Load = (int) Math.Round(
                memoryHardwareSensors.First(s => s.Name == "Memory" && s.SensorType == SensorType.Load)
                    .Value.GetFloatAsCorrectNumber(),
                0,
                MidpointRounding.AwayFromZero);
            memoryInfo.Available = (double) Math.Round(
                memoryHardwareSensors
                    .First(s => s.Name.Contains("Memory Available") && s.SensorType == SensorType.Data)
                    .Value.GetFloatAsCorrectNumber(),
                1,
                MidpointRounding.AwayFromZero);
            memoryInfo.Used = (double) Math.Round(
                memoryHardwareSensors
                    .First(s => s.Name.Contains("Memory Used") && s.SensorType == SensorType.Data)
                    .Value.GetFloatAsCorrectNumber(),
                1,
                MidpointRounding.AwayFromZero);

            return memoryInfo;
        }
        catch (Exception exception)
        {
            _logger.Error(exception);
            return memoryInfo;
        }
    }
}