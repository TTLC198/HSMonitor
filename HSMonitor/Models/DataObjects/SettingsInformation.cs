using HSMonitor.Models.Enums;

namespace HSMonitor.Models.DataObjects;

public class SettingsData : Data
{
    public SettingsData(SettingsMessage message) : base(message, InformationType.SaveSettings)
    {
    }
}

public class SettingsMessage : IMessage
{
    public int DisplayBrightness { get; set; }
}