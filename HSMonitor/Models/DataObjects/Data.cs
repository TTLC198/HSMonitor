using HSMonitor.Models.Enums;

namespace HSMonitor.Models.DataObjects;

public class Data
{
    public InformationType Type { get; set; }
    public IMessage Message { get; set; }

    public Data(IMessage message, InformationType type)
    {
        Message = message;
        Type = type;
    }
}