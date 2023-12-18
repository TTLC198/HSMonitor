using System.Text.Json.Serialization;

namespace HSMonitor.Models.DataObjects;

[JsonDerivedType(typeof(HardwareMessage))]
[JsonDerivedType(typeof(SettingsMessage))]
public interface IMessage
{
    
}