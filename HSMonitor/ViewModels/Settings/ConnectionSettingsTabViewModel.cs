using System.Collections.Generic;
using System.Linq;
using HSMonitor.Models;
using HSMonitor.Services;
using HSMonitor.Utils.Usb.Serial;

namespace HSMonitor.ViewModels.Settings;

public class ConnectionSettingsTabViewModel : SettingsTabBaseViewModel
{
    private IEnumerable<DeviceInfo> _availablePorts = Serial.GetPorts();

    public ConnectionSettingsTabViewModel(SettingsService settingsService, Serial serial)
        : base(settingsService, 0, "Connection")
    {
    }

    public IEnumerable<DeviceInfo> AvailablePorts
    {
        get => _availablePorts;
        private set
        {
            _availablePorts = value;
            OnPropertyChanged(nameof(AvailablePorts));
        }
    }

    public static IEnumerable<int> SupportedBaudRates => new List<int>
    {
        300,
        600,
        1200,
        2400,
        4800,
        9600,
        19200,
        38400,
        57600,
        115200,
        230400,
        460800,
        921600
    };

    public DeviceInfo SelectedDevice
    {
        get =>
            AvailablePorts.FirstOrDefault(p => p.PortName ==
                                               (SettingsService.Settings.LastSelectedPort ?? "COM1")) ??
            new DeviceInfo();
        set => SettingsService.Settings.LastSelectedPort = value.PortName;
    }

    public int SelectedBaudRate
    {
        get => SettingsService.Settings.LastSelectedBaudRate;
        set => SettingsService.Settings.LastSelectedBaudRate = value;
    }

    public int SendInterval
    {
        get => SettingsService.Settings.SendInterval;
        set => SettingsService.Settings.SendInterval = value;
    }

    public int DeviceDisplayBrightness
    {
        get => SettingsService.Settings.DeviceDisplayBrightness;
        set => SettingsService.Settings.DeviceDisplayBrightness = value;
    }

    public void UpdateAvailablePorts()
    {
        AvailablePorts = new List<DeviceInfo>();
        AvailablePorts = Serial.GetPorts();
    }
}