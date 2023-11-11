namespace HSMonitor.Models;

public class DeviceInfo
{
    public string? PortName { get; set; }
    public string? Description { get; set; }
    public string? BusDescription { get; set; }
    public bool IsHsMonitorDevice { get; set; } = false;
}