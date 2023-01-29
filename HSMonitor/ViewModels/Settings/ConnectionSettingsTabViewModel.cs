using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using HSMonitor.Services;
using HSMonitor.Utils.Serial;

namespace HSMonitor.ViewModels.Settings;

public class ConnectionSettingsTabViewModel : SettingsTabBaseViewModel
{
    public IEnumerable<string> AvailablePorts 
        => SerialPort.GetPortNames();

    public static IEnumerable<int> SupportedBaudRates => new List<int>()
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

    public string SelectedPort
    {
        get => SettingsService.Settings.LastSelectedPort ?? AvailablePorts.FirstOrDefault() ?? "COM1";
        set => SettingsService.Settings.LastSelectedPort = value;
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
    
    public ConnectionSettingsTabViewModel(SettingsService settingsService) 
        : base(settingsService, 0, "Connection")
    {
        if (SettingsService is {Settings: not null}) 
            SelectedPort = SettingsService.Settings.LastSelectedPort ?? (AvailablePorts.FirstOrDefault() ?? "COM1");
    }
}