using System.Text.Json;
using HSMonitor.Services.SerialDataService.Parts;
using HSMonitor.Utils.Logger;
using HSMonitor.Services.HardwareMonitorService;

namespace HSMonitor.Services.SerialDataService;

public class SerialDataService : IDisposable
{
    private readonly HardwareMonitorServiceImpl _hardwareMonitorServiceImpl;
    private readonly SettingsService _settingsService;
    private readonly ILogger<SerialDataService> _logger;

    private Serial _serialData;
    public event EventHandler? OpenPortAttemptFailed;
    public event EventHandler? OpenPortAttemptSuccessful;

    public SerialDataService(
        SettingsService settingsService,
        HardwareMonitorServiceImpl hardwareMonitorServiceImpl,
        ILogger<SerialDataService> logger)
    {
        _settingsService = settingsService;
        _hardwareMonitorServiceImpl = hardwareMonitorServiceImpl;
        _logger = logger;
        _serialData = new Serial(settingsService);

        _settingsService.SettingsSaved += (_, _) => UpdateSerialSettings();
        _hardwareMonitorServiceImpl.HardwareInformationUpdated += SendInformationToMonitor;
    }

    private void UpdateSerialSettings()
    {
        _serialData.Dispose();
        _serialData = new Serial(_settingsService ?? throw new InvalidOperationException());
    }

    private void SendInformationToMonitor(object? sender, EventArgs args)
    {
        if (sender is not HardwareMonitorServiceImpl hardwareMonitorService) return;
        var message = hardwareMonitorService.GetHwInfoMessage() ?? throw new Exception("Message empty");
        if (_settingsService.Settings.IsDeviceBackwardCompatibilityEnabled)
        {
            if (message.CpuInformation is {Name.Length: > 23})
                message.CpuInformation.Name = message.CpuInformation.Name[..23];
            if (message.GpuInformation is {Name.Length: > 23})
                message.GpuInformation.Name = message.GpuInformation.Name[..23];
        }
        
        var jsonMessage = JsonSerializer.Serialize(message);
        jsonMessage += "\n\0";
        var jsonData = jsonMessage
            .Select(s => (byte) s);
        
        if (_serialData.CheckAccess())
        {
            try
            {
                _serialData.Write(jsonData.ToArray());
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

    public void Dispose() => _serialData.Dispose();
}