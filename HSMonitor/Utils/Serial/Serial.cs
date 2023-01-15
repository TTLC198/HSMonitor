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
        _serialPort = new SerialPort(
            string.IsNullOrEmpty(_settingsService.Settings.LastSelectedPort) ? "COM1" : _settingsService.Settings.LastSelectedPort,
            _settingsService.Settings.LastSelectedBaudRate,
            Parity.None, 
            8,
            StopBits.One);
        _serialPort.ReadTimeout = 500;
        _serialPort.WriteTimeout = 500;
        _serialPort.DtrEnable = true;
        _serialPort.RtsEnable = true;
    }

    public bool Open()
    {
        if (_serialPort.IsOpen)
            return true;

        _serialPort.PortName = _settingsService.Settings.LastSelectedPort ?? "COM1";
        _serialPort.Open();

        return true;
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
    
    public void Write(string text)
    {
        if (!_serialPort.IsOpen) return;
        try
        {
            _serialPort.Write(text);
        }
        catch (Exception ex)
        {
            try { _serialPort.Close(); }
            catch { }

            Open();
        }
    }

    public void Dispose()
    {
        Close();
        _serialPort.Dispose();
    }
}