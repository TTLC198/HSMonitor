using System;
using System.IO;
using System.IO.Ports;
using HSMonitor.Services;

namespace HSMonitor.Utils.Serial;

public class Serial : IDisposable
{
    private readonly SerialPort _serial;
    private readonly SettingsService _settingsService;

    public Serial(SettingsService settingsService)
    {
        _settingsService = settingsService;
        _serial = new SerialPort(string.IsNullOrEmpty(_settingsService.Settings.LastSelectedPort) ? "COM1" : _settingsService.Settings.LastSelectedPort, _settingsService.Settings.LastSelectedBaudRate);
    }

    public bool Open()
    {
        if (_serial.IsOpen)
        {
            try { _serial.Close(); }
            catch { }
        }

        _serial.PortName = _settingsService.Settings.LastSelectedPort ?? "COM1";

        try
        {
            _serial.Open();
        }
        catch (IOException e)
        {
            return false;
        }

        return true;
    }

    private void Close()
    {
        if (_serial.IsOpen)
        {
            _serial.Close();
        }
    }

    public void Write(byte[] data)
    {
        if (_serial.IsOpen)
        {
            try
            {
                _serial.Write(data, 0, data.Length);
            }
            catch (Exception ex)
            {
                try { _serial.Close(); }
                catch { }

                Open();
            }
        }
    }

    public void Dispose()
    {
        Close();
        _serial.Dispose();
    }
}