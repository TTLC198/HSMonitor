using System;
using System.Linq;
using System.Text.Json;
using HSMonitor.Utils.Logger;
using HSMonitor.Utils.Serial;

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
        _hardwareMonitorService.HardwareInformationUpdated += (_, _) => SendInformationToMonitor();
    }

    private void UpdateSerialSettings()
    {
        _serial.Dispose();
        _serial = new Serial(_settingsService ?? throw new InvalidOperationException());
    }

    private void SendInformationToMonitor()
    {
        var message = _hardwareMonitorService.GetHwInfoMessage() ?? throw new Exception("Message empty");
        if (message.CpuInformation is {Name.Length: > 23})
            message.CpuInformation.Name = message.CpuInformation.Name[..23];
        if (message.GpuInformation is {Name.Length: > 23})
            message.GpuInformation.Name = message.GpuInformation.Name[..23];
        var jsonData = JsonSerializer
            .Serialize(message)
            .Select(s => (byte) s);
        if (!_settingsService.Settings.IsDeviceBackwardCompatibilityEnabled)
            jsonData = jsonData.Append((byte)'\0');
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