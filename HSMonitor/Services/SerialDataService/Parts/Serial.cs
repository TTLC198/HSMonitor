using System.IO.Ports;
using HSMonitor.Models;

namespace HSMonitor.Services.SerialDataService.Parts;

public class Serial : IDisposable
{
    private readonly SerialPort _serialPort;
    private readonly SettingsService _settingsService;

    public Serial(SettingsService settingsService)
    {
        _settingsService = settingsService;
        
        var port = string.IsNullOrEmpty(_settingsService.Settings?.LastSelectedPort)
            ? GetPorts().FirstOrDefault(d => d.IsHsMonitorData)?.PortName
            : _settingsService.Settings?.LastSelectedPort;
        
        _serialPort = new SerialPort(
            port,
            _settingsService.Settings?.LastSelectedBaudRate ?? 9600,
            Parity.None,
            8,
            StopBits.One);
    }

    public static IEnumerable<DeviceInfo> GetPorts()
    {
        var devices = Win32DeviceMgmt
            .GetAllCOMPorts();
        
        //todo: пересмотреть
        devices
            .Where(d => d.BusDescription?.Contains("HSMonitor Data") == true)
            .ToList()
            .ForEach(d => d.IsHsMonitorData = true);
        
        devices
            .Where(d => d.BusDescription?.Contains("HSMonitor OTA") == true)
            .ToList()
            .ForEach(d => d.IsHsMonitorOta = true);

        return devices;
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

    private bool Open()
    {
        if (_serialPort.IsOpen)
            return true;

        try
        {
            _serialPort.PortName = _settingsService.Settings.LastSelectedPort ?? "COM1";
            _serialPort.Open();
        }
        catch
        {
            _serialPort.Close();
            throw;
        }

        return _serialPort.IsOpen;
    }

    public void Close()
    {
        if (!_serialPort.IsOpen) return;
        _serialPort.Close();
    }

    public void Write(byte[] data)
    {
        if (!_serialPort.IsOpen) return;
        try
        {
            _serialPort.Write(data, 0, data.Length);
        }
        catch
        {
            _serialPort.Close();
            throw;
        }
    }

    public void Dispose()
    {
        Close();
        _serialPort.Dispose();
    }
        
}