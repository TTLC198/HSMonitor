using HSMonitor.Models.Enums;

namespace HSMonitor.Models.DataObjects;


public class FirmwareUpdateData : Data
{
    public FirmwareUpdateData(FirmwareUpdateMessage message) : base(message, InformationType.FirmwareUpdate)
    {
        
    }
}

public class FirmwareUpdateMessage : IMessage
{
    public int PacketNumber { get; set; }
    public string Version { get; set; }
    public byte[] Data { get; set; }
}