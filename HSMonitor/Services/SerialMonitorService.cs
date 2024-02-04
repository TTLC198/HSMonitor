using System;
using System.Linq;
using System.Text.Json;
using HSMonitor.Models.DataObjects;
using HSMonitor.Utils.Logger;
using HSMonitor.Utils.Serial;
using HSMonitor.Utils.Usb.Serial;

namespace HSMonitor.Services;

public class SerialMonitorService : IDisposable
{
    private readonly HardwareMonitorService _hardwareMonitorService;
    private readonly SettingsService _settingsService;
    private readonly ILogger<SerialMonitorService> _logger;

    private Serial _serial;
    public event EventHandler? OpenPortAttemptFailed;
    public event EventHandler? OpenPortAttemptSuccessful;

    public SerialMonitorService(
        SettingsService settingsService,
        HardwareMonitorService hardwareMonitorService,
        ILogger<SerialMonitorService> logger)
    {
        _settingsService = settingsService;
        _hardwareMonitorService = hardwareMonitorService;
        _logger = logger;
        _serial = new Serial(settingsService);

        _settingsService.SettingsSaved += (_, _) => UpdateSerialSettings();
        _hardwareMonitorService.HardwareInformationUpdated += SendHardwareInformation;
    }

    private void UpdateSerialSettings()
    {
        _serial.Dispose();
        _serial = new Serial(_settingsService ?? throw new InvalidOperationException());
        var settingsData = new SettingsData(new SettingsMessage()
        {
            DisplayBrightness = _settingsService.Settings.DeviceDisplayBrightness
        });
        SendInformation(settingsData);
    }
    
    public void SendUpdateFirmwarePacket(Data firmwareUpdateData)
    {
        _serial.Dispose();
        _serial = new Serial(_settingsService ?? throw new InvalidOperationException());
        SendInformation(firmwareUpdateData);
    }

    private void SendHardwareInformation(object? sender, EventArgs args)
    {
        if (sender is not HardwareMonitorService hardwareMonitorService) return;
        var message = hardwareMonitorService.GetHwInfoMessage() ?? throw new Exception("Message empty");
        if (_settingsService.Settings.IsDeviceBackwardCompatibilityEnabled)
        {
            if (message.CpuInformation is {Name.Length: > 23})
                message.CpuInformation.Name = message.CpuInformation.Name[..23];
            if (message.GpuInformation is {Name.Length: > 23})
                message.GpuInformation.Name = message.GpuInformation.Name[..23];
        }
        var data = new HardwareData(message);
        SendInformation(data);
    }

    public void SendInformation(Data data)
    {
#if DEBUG
        var temp = JsonSerializer //todo remove after testing
            .Serialize(data); 
#endif
        var jsonData = JsonSerializer
            .Serialize(data)
            .Select(s => (byte) s);
        if (_serial.CheckAccess())
        {
            try
            {
                _serial.Write(jsonData.ToArray());
                OpenPortAttemptSuccessful?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception exception)
            {
                _logger.Error(exception);
                OpenPortAttemptFailed?.Invoke(this, EventArgs.Empty);
            }
        }
        else
        {
            OpenPortAttemptFailed?.Invoke(this, EventArgs.Empty);
        }
    }

    public void Dispose() => _serial.Dispose();
}