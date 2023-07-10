using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using HSMonitor.Models;
using HSMonitor.Properties;
using HSMonitor.Utils;
using HSMonitor.Utils.Logger;
using HSMonitor.ViewModels;
using HSMonitor.ViewModels.Framework;

namespace HSMonitor.Services;

public class SettingsService
{
    private readonly IViewModelFactory _viewModelFactory;
    private readonly DialogManager _dialogManager;
    [UnconditionalSuppressMessage("SingleFile", "IL3002:Avoid calling members marked with 'RequiresAssemblyFilesAttribute' when publishing as a single-file", Justification = "<Pending>")]
    private readonly RegistryHandler _autoStartSwitch = new(
        "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", 
        $"\"{App.ExecutableFilePath}\"",
        new [] {App.HiddenOnLaunchArgument}
    );

    public event EventHandler? SettingsReset;
    
    public event EventHandler? SettingsLoaded;
    
    public event EventHandler? SettingsSaved;
    
    public ApplicationSettings Settings { get; set; } = null!;

    public readonly string ConfigurationPath = Path.Combine(App.SettingsDirPath, "appsettings.json");
    private readonly ILogger<SettingsService> _logger;

    public SettingsService(IViewModelFactory viewModelFactory, DialogManager dialogManager, ILogger<SettingsService> logger)
    {
        _viewModelFactory = viewModelFactory;
        _dialogManager = dialogManager;
        _logger = logger;
        Load().Wait();
    }
    
    public async Task Reset()
    {
        Settings = new ApplicationSettings()
        {
            LastSelectedPort = null,
            LastSelectedBaudRate = 115200,
            SendInterval = 1000,
            DeviceDisplayBrightness = 100,
            CpuId = "",
            GpuId = "",
            CpuCustomName = "",
            GpuCustomName = "",
            CpuCustomType = "",
            GpuCustomType = "",
            IsAutoDetectHardwareEnabled = true,
            IsHiddenAutoStartEnabled = true,
            IsAutoStartEnabled = false,
            ApplicationCultureInfo = CultureInfo.InstalledUICulture.Name
        };
        await Save();
        SettingsReset?.Invoke(this, EventArgs.Empty);
    }

    public async Task Load()
    {
        try
        {
            var json = File.ReadAllText(ConfigurationPath);
            Settings = JsonSerializer.Deserialize<ApplicationSettings>(json) ?? throw new InvalidOperationException();
            Settings.IsAutoStartEnabled = _autoStartSwitch.IsSet;
            Settings.LastSelectedPort ??= SerialPort.GetPortNames().FirstOrDefault() ?? "COM1";
            SettingsLoaded?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception exception)
        {
            _logger.Error(exception);
            
            var messageBoxDialog = _viewModelFactory.CreateMessageBoxViewModel(
                title: Resources.MessageBoxErrorTitle,
                message: $@"
{Resources.MessageBoxErrorText}
{exception.Message.Split('\'').Last()}".Trim(),
                okButtonText: "OK",
                cancelButtonText: null
            );

            if (await _dialogManager.ShowDialogAsync(messageBoxDialog) == true)
                Application.Current.Shutdown();
        }
    }

    public async Task Save()
    {
        Settings.JsonToFile(ConfigurationPath);
        
        try
        {
            if (LocalizationManager.GetCurrentCulture().Name != Settings.ApplicationCultureInfo)
            {
                var messageBoxDialog = _viewModelFactory.CreateMessageBoxViewModel(
                    title: Resources.RestartRequirementMessageTitle,
                    message: Resources.RestartRequirementMessageText.Trim(),
                    okButtonText: Resources.MessageBoxOkButtonText,
                    cancelButtonText: Resources.MessageBoxCancelButtonText
                );

                if (await _dialogManager.ShowDialogAsync(messageBoxDialog) == true)
                    RestartApplication();
            }
            
            if (Settings.IsAutoStartEnabled)
            {
                if (Settings.IsHiddenAutoStartEnabled)
                {
                    if (_autoStartSwitch.Args.FirstOrDefault(a => a.Contains(App.HiddenOnLaunchArgument)) is null)
                        _autoStartSwitch.Args = _autoStartSwitch.Args.Append(App.HiddenOnLaunchArgument).ToArray();
                }
                else
                {
                    if (_autoStartSwitch.Args.FirstOrDefault(a => a.Contains(App.HiddenOnLaunchArgument)) is not null)
                        _autoStartSwitch.Args = _autoStartSwitch.Args.Where(a => !a.Contains(App.HiddenOnLaunchArgument))
                            .ToArray();
                }
            }
            _autoStartSwitch.IsSet = Settings.IsAutoStartEnabled;
        }
        catch (Exception exception)
        {
            _logger.Error(exception);
            
            var messageBoxDialog = _viewModelFactory.CreateMessageBoxViewModel(
                title: Resources.MessageBoxErrorTitle,
                message: $@"
{Resources.MessageBoxErrorText}
{exception.Message.Split('\'').Last()}".Trim(),
                okButtonText: "OK",
                cancelButtonText: null
            );
            await _dialogManager.ShowDialogAsync(messageBoxDialog);
        }
        
        SettingsSaved?.Invoke(this, EventArgs.Empty);
    }
    
    private async void RestartApplication()
    {
        var startInfo = new ProcessStartInfo
        {
            UseShellExecute = true,
            WorkingDirectory = Environment.CurrentDirectory,
            FileName = App.ExecutableFilePath,
            Arguments = "restart"
        };

        try
        {
            Process.Start(startInfo);
            Application.Current.Shutdown();
        }
        catch (Exception exception)
        {
            _logger.Error(exception);
            
            var messageBoxDialog = _viewModelFactory.CreateMessageBoxViewModel(
                title: Resources.MessageBoxErrorTitle,
                message: $@"
{Resources.MessageBoxErrorText}
{exception.Message.Split('\'').Last()}".Trim(),
                okButtonText: Resources.MessageBoxOkButtonText,
                cancelButtonText: null
            );
            await _dialogManager.ShowDialogAsync(messageBoxDialog);
        }
    }
}