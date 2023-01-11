using System.Text.Json.Serialization;

namespace HSMonitor.Models;

public class ApplicationSettings
{
    // General
    
    [JsonPropertyName("SerialPort")]
    public string? LastSelectedPort { get; set; }

    [JsonPropertyName("BaudRate")]
    public int LastSelectedBaudRate { get; set; }
    
    public int SendInterval { get; set; }
    
    // Customization

    public string? CpuCustomName { get; set; }
    
    public string? GpuCustomName { get; set; }

    public bool IsAutodetectHardwareEnabled { get; set; }
    
    // Advanced
    [JsonIgnore]
    public bool IsAutoStartEnabled { get; set; }
}