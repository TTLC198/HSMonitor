using System;
using System.IO;
using System.Runtime.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using HSMonitor.Models;
using HSMonitor.Utils;
using HSMonitor.Utils.Serial;
using HSMonitor.ViewModels;
using HSMonitor.ViewModels.Framework;

namespace HSMonitor.Services;

public class SettingsService
{
    private readonly IViewModelFactory _viewModelFactory;
    private readonly DialogManager _dialogManager;
    private readonly RegistryHandler _autoStartSwitch = new(
        "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", 
        $"\"{App.ExecutableFilePath}\" {App.HiddenOnLaunchArgument}"
    );

    public event EventHandler? SettingsReset;
    
    public event EventHandler? SettingsLoaded;
    
    public event EventHandler? SettingsSaved;
    
    public ApplicationSettings Settings { get; private set; }

    private readonly string _configurationPath = Path.Combine(App.ExecutableDirPath, "appsettings.json");

    public SettingsService(IViewModelFactory viewModelFactory, DialogManager dialogManager)
    {
        _viewModelFactory = viewModelFactory;
        _dialogManager = dialogManager;
        Load();
    }
    
    public void Reset()
    {
        Settings = new ApplicationSettings()
        {
            LastSelectedPort = null,
            LastSelectedBaudRate = 115200,
            SendInterval = 1000,
            CpuCustomName = null,
            GpuCustomName = null,
            IsAutodetectHardwareEnabled = true,
            IsAutoStartEnabled = true
        };
        Save();
        SettingsReset?.Invoke(this, EventArgs.Empty);
    }

    public void Load()
    {
        var json = File.ReadAllText(_configurationPath);
        Settings = json.JsonToItem<ApplicationSettings>() ?? throw new InvalidOperationException();
        Settings.IsAutoStartEnabled = _autoStartSwitch.IsSet;
        SettingsLoaded?.Invoke(this, EventArgs.Empty);
    }

    public void Save()
    {
        Settings.JsonToFile(_configurationPath);
        
        try
        {
            _autoStartSwitch.IsSet = Settings.IsAutoStartEnabled;
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

            Task.Run(async () =>
            {
                await _dialogManager.ShowDialogAsync(messageBoxDialog);
            });
        }
        
        SettingsSaved?.Invoke(this, EventArgs.Empty);
    }
}