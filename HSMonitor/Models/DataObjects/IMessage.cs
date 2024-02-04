using System.Text.Json.Serialization;
using HSMonitor.Services;

namespace HSMonitor.Models.DataObjects;

[JsonDerivedType(typeof(HardwareMessage))]
[JsonDerivedType(typeof(SettingsMessage))]
[JsonDerivedType(typeof(FirmwareUpdateMessage))]
public interface IMessage
{
    
}