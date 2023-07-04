using System.Text.Json.Serialization;
using HSMonitor.Services;

namespace HSMonitor.Models;

public class ApplicationSettings
{
    // General
    
    [JsonPropertyName("SerialPort")]
    public string? LastSelectedPort { get; set; }

    [JsonPropertyName("BaudRate")]
    public int LastSelectedBaudRate { get; set; }
    
    public int SendInterval { get; set; }
    
    public int DeviceDisplayBrightness { get; set; }
    
    // Hardware
    
    public string? CpuId { get; set; }
    
    public string? GpuId { get; set; }
    
    public int DefaultCpuFrequency { get; set; }
    
    public int DefaultGpuFrequency { get; set; }
    
    // Customization

    public string? CpuCustomName { get; set; }

    public string? CpuCustomType { get; set; }
    
    public string? GpuCustomName { get; set; }
    
    public string? GpuCustomType { get; set; }
    
    public string? MemoryCustomType { get; set; }
    
    public bool IsAutoDetectHardwareEnabled { get; set; }
    
    // Advanced
    [JsonIgnore]
    public bool IsAutoStartEnabled { get; set; }
    public string? ApplicationCultureInfo { get; set; }
    public bool IsHiddenAutoStartEnabled { get; set; }
    public bool IsRunAsAdministrator { get; set; }
    
    // Update
    
    public bool IsAutoUpdateEnabled { get; set; }
}