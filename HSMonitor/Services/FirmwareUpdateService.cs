using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HSMonitor.Models.DataObjects;
using HSMonitor.Services.Update;
using HSMonitor.Utils.Logger;
using HSMonitor.ViewModels.Framework;

namespace HSMonitor.Services;

public class FirmwareUpdateService : IUpdateService
{
    private readonly IViewModelFactory _viewModelFactory;
    private readonly DialogManager _dialogManager;
    private readonly ILogger<FirmwareUpdateService> _logger;
    private readonly SerialMonitorService _serialMonitorService;

    public FirmwareUpdateService(IViewModelFactory viewModelFactory, DialogManager dialogManager, ILogger<FirmwareUpdateService> logger, SerialMonitorService serialMonitorService)
    {
        _viewModelFactory = viewModelFactory;
        _dialogManager = dialogManager;
        _logger = logger;
        _serialMonitorService = serialMonitorService;
    }

    public async Task UpdateAsync()
    {
        var packetSize = 512;
        var tempFile = @"C:\Users\yoreh\CLionProjects\HSMonitor\build\test.txt";
        if (File.Exists(tempFile))
        {
            var bytesArray = await File.ReadAllBytesAsync(tempFile);
            var packetNumber = 0;      
            do
            {
                var updateData = new FirmwareUpdateData(new FirmwareUpdateMessage()
                {
                    Version = "", //todo
                    PacketNumber = packetNumber,
                    Data = bytesArray
                        .Skip(packetNumber * packetSize)
                        .Take(packetSize)
                        .ToArray()
                });
                var t = bytesArray.Length / packetSize; //todo
                _serialMonitorService.SendUpdateFirmwarePacket(updateData);
                packetNumber++;
            } while (packetNumber < bytesArray.Length / packetSize);
        }
    }
}