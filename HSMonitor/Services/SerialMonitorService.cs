using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using HSMonitor.Models;
using HSMonitor.Utils.Serial;
using HSMonitor.ViewModels;
using HSMonitor.ViewModels.Framework;

namespace HSMonitor.Services;

public class SerialMonitorService : IDisposable
{
    private readonly Serial _serial;
    private readonly IViewModelFactory _viewModelFactory;
    private readonly DialogManager _dialogManager;

    public SerialMonitorService(
        SettingsService settingsService,
        IViewModelFactory viewModelFactory,
        DialogManager dialogManager)
    {
        _viewModelFactory = viewModelFactory;
        _dialogManager = dialogManager;
        _serial = new Serial(settingsService);
    }

    ~SerialMonitorService() => Dispose();

    public bool SendInformationToMonitor()
    {
        var isConnected = false;

        try
        {
            var message = HardwareMonitorService.GetHwInfoMessage() ?? throw new Exception("Message empty");
            var jsonData = JsonSerializer
                .Serialize(message)
                .Select(s => (byte)s)
                .ToArray();
            if (_serial.Open())
                _serial.Write(jsonData);
        }
        catch (Exception exception)
        {
            var messageBoxDialog = _viewModelFactory.CreateMessageBoxViewModel(
                title: "Some error has occurred",
                message: $@"
An error has occurred, the error text is shown below
{exception.Message}".Trim(),
                okButtonText: "OK",
                cancelButtonText: null
            );
            if (_dialogManager.ShowDialogAsync(messageBoxDialog).Result == true)
            {
                _serial.Close();
            }
        }

        return isConnected;
    }

    public void Dispose()
    {
        _serial.Dispose();
    }
}