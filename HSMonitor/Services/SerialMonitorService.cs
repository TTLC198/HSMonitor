using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using HSMonitor.Models;
using HSMonitor.Utils.Serial;
using HSMonitor.ViewModels;
using HSMonitor.ViewModels.Framework;

namespace HSMonitor.Services;

public class SerialMonitorService : IDisposable
{
    private readonly HardwareMonitorService _hardwareMonitorService;
    private readonly SettingsService _settingsService;

    private Serial _serial;
    public event EventHandler? OpenPortAttemptFailed;
    public event EventHandler? OpenPortAttemptSuccessful;

    public SerialMonitorService(
        SettingsService settingsService,
        HardwareMonitorService hardwareMonitorService)
    {
        _settingsService = settingsService;
        _hardwareMonitorService = hardwareMonitorService;
        _serial = new Serial(settingsService);

        _settingsService.SettingsSaved += (_, _) => UpdateSerialSettings();
        settingsService.SettingsReset += (_, _) => UpdateSerialSettings();
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
        var jsonData = JsonSerializer
            .Serialize(message)
            .Select(s => (byte)s)
            .ToArray();
        if (_serial.CheckAccess())
        {
            _serial.Write(jsonData);
            OpenPortAttemptSuccessful?.Invoke(this, EventArgs.Empty);
        }
        else
        {
            OpenPortAttemptFailed?.Invoke(this, EventArgs.Empty);
        }
    }

    public void Dispose()
    {
        _serial.Dispose();
    }
    
    ~SerialMonitorService() => Dispose();
}