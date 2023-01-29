using System;
using System.IO;
using System.IO.Ports;
using HSMonitor.Services;

namespace HSMonitor.Utils.Serial;

public class Serial : IDisposable
{
    private readonly SerialPort _serialPort;
    private readonly SettingsService _settingsService;

    public Serial(SettingsService settingsService)
    {
        _settingsService = settingsService;
        if (_settingsService is {Settings: not null}) 
            _serialPort = new SerialPort(
                string.IsNullOrEmpty(_settingsService.Settings.LastSelectedPort) ? "COM1" : _settingsService.Settings.LastSelectedPort,
                _settingsService.Settings.LastSelectedBaudRate,
                Parity.None, 
                8,
                StopBits.One);
    }

    public bool CheckAccess()
    {
        try
        {
            return Open();
        }
        catch
        {
            return false;
        }
    }

    public bool Open()
    {
        if (_serialPort.IsOpen)
            return true;

        _serialPort.PortName = _settingsService.Settings.LastSelectedPort ?? "COM1";
        _serialPort.Open();

        return _serialPort.IsOpen;
    }

    public void Close()
    {
        if (_serialPort.IsOpen)
            _serialPort.Close();
    }

    public void Write(byte[] data)
    {
        if (!_serialPort.IsOpen) return;
        _serialPort.Write(data, 0, data.Length);
    }

    public void Dispose()
    {
        if (_serialPort is not null)
            _serialPort.Dispose();
    }
        
}