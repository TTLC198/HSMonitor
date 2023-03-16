using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using HSMonitor.Services;
using HSMonitor.Utils;
using HSMonitor.ViewModels.Framework;
using HSMonitor.ViewModels.Settings;
using NetSparkleUpdater.Enums;
using Application = System.Windows.Application;
using Screen = Stylet.Screen;

namespace HSMonitor.ViewModels;

#pragma warning disable CA1416
public class MainWindowViewModel : Screen
{
    private readonly IViewModelFactory _viewModelFactory;
    private readonly DialogManager _dialogManager;
    private readonly SettingsService _settingsService;
    private readonly SerialMonitorService _serialMonitorService;
    private readonly HardwareMonitorService _hardwareMonitorService;
    private readonly UpdateService _updateService;

    private DispatcherTimer _updateHardwareMonitorTimer = null!;
    public DashboardViewModel Dashboard { get; }

    private bool _isSerialMonitorEnabled = true;

    public bool IsSerialMonitorEnabled
    {
        get => _isSerialMonitorEnabled;
        set
        {
            _isSerialMonitorEnabled = value;
            OnPropertyChanged(nameof(IsSerialMonitorEnabled));
        }
    }

    public MainWindowViewModel(
        IViewModelFactory viewModelFactory,
        DialogManager dialogManager,
        HardwareMonitorService hardwareMonitorService,
        SettingsService settingsService,
        SerialMonitorService serialMonitorService, UpdateService updateService)
    {
        _viewModelFactory = viewModelFactory;
        _dialogManager = dialogManager;
        _hardwareMonitorService = hardwareMonitorService;
        _settingsService = settingsService;
        _serialMonitorService = serialMonitorService;
        _updateService = updateService;

        Dashboard = viewModelFactory.CreateDashboardViewModel();
        DisplayName = $"{App.Name} v{App.VersionString}";
    }

    private async Task SerialMonitorServiceOnOpenPortAttemptFailed()
    {
        if (!IsSerialMonitorEnabled)
            return;

        var messageBoxDialog = _viewModelFactory.CreateMessageBoxViewModel(
            title: $"Port {_settingsService.Settings.LastSelectedPort} is busy!",
            message: @"Please connect a device or choose a different port.",
            okButtonText: "OK",
            cancelButtonText: "CANCEL"
        );

        IsSerialMonitorEnabled = false;

        if (await _dialogManager.ShowDialogAsync(messageBoxDialog) == true)
        {
            var settingsDialog = _viewModelFactory.CreateSettingsViewModel();
            settingsDialog.ActivateTabByType<ConnectionSettingsTabViewModel>();

            await _dialogManager.ShowDialogAsync(settingsDialog);
        }
    }

    private async Task ShowAdminPrivilegesRequirement()
    {
        var messageBoxDialog = _viewModelFactory.CreateMessageBoxViewModel(
            title: "Less information about PC",
            message: $@"
Please run {App.Name} as an administrator so that the library can get complete information about the current state of the computer hardware.

Press OK to restart the application as administrator.".Trim(),
            okButtonText: "OK",
            cancelButtonText: "CANCEL"
        );

        if (await _dialogManager.ShowDialogAsync(messageBoxDialog) == true)
            RestartAsAdmin();
    }

    private void RestartAsAdmin()
    {
        var startInfo = new ProcessStartInfo
        {
            UseShellExecute = true,
            WorkingDirectory = Environment.CurrentDirectory,
            FileName = App.ExecutableFilePath,
            Arguments = "restart" + (App.IsHiddenOnLaunch ? " " + App.HiddenOnLaunchArgument : null),
            Verb = "runas"
        };

        try
        {
            Process.Start(startInfo);
            Exit();
        }
        catch (Exception exception)
        {
            var messageBoxDialog = _viewModelFactory.CreateMessageBoxViewModel(
                title: "Some error has occurred",
                message: $@"
An error has occurred, the error text is shown below:
{exception.Message.Split('\'').Last()}".Trim(),
                okButtonText: "OK",
                cancelButtonText: null
            );
            _dialogManager.ShowDialogAsync(messageBoxDialog).GetAwaiter();
        }
    }

    public async void OnViewFullyLoaded()
    {
        if (!File.Exists(_settingsService.ConfigurationPath) || _settingsService is {Settings: null})
        {
            var messageBoxDialog = _viewModelFactory.CreateMessageBoxViewModel(
                title: "Some error has occurred",
                message: $@"
Application configuration file was not found in the program folder or was modified manually.
Please reinstall the program to fix the problem".Trim(),
                okButtonText: "OK",
                cancelButtonText: null
            );

            if (await _dialogManager.ShowDialogAsync(messageBoxDialog) == true)
                Exit();
        }
        else
        {
            _updateHardwareMonitorTimer = new DispatcherTimer(
                priority: DispatcherPriority.Background,
                interval: TimeSpan.FromMilliseconds(_settingsService.Settings.SendInterval == 0
                    ? 500
                    : _settingsService.Settings.SendInterval),
                callback: (_, _) => { _hardwareMonitorService.HardwareInformationUpdate(); },
                dispatcher: Dispatcher.FromThread(Thread.CurrentThread) ?? throw new InvalidOperationException()
            );

            _settingsService.SettingsSaved += (_, _) =>
            {
                _updateHardwareMonitorTimer.Interval = TimeSpan.FromMilliseconds(
                    _settingsService.Settings.SendInterval == 0 ? 500 : _settingsService.Settings.SendInterval);
            };

            _serialMonitorService.OpenPortAttemptFailed +=
                async (_, _) => await SerialMonitorServiceOnOpenPortAttemptFailed();

            _serialMonitorService.OpenPortAttemptSuccessful += (_, _) =>
            {
                if (IsSerialMonitorEnabled)
                    return;
                IsSerialMonitorEnabled = true;
            };

            try
            {
                var updateInfo = await _updateService.CheckForUpdates();
                
                if (updateInfo.Status is UpdateStatus.UpdateAvailable)
                {
                    if (_settingsService.Settings.IsAutoUpdateEnabled)
                    {
                        var settingsDialog = _viewModelFactory.CreateSettingsViewModel();
                        settingsDialog.ActivateTabByType<UpdateSettingsTabViewModel>();
                        
                        await _updateService.UpdateAsync();
                    }
                    else
                    {
                        var messageBoxDialog = _viewModelFactory.CreateMessageBoxViewModel(
                            title: "New update available!",
                            message: $@"
There is new version {updateInfo.Updates.First().Version} available.         
You are using version {App.Version.ToString(3).Trim()}.
Do you want to update the application now?".Trim(),
                            okButtonText: "OK",
                            cancelButtonText: "Cancel"
                        );
                        if (await _dialogManager.ShowDialogAsync(messageBoxDialog) == true)
                        {
                            var settingsDialog = _viewModelFactory.CreateSettingsViewModel();
                            settingsDialog.ActivateTabByType<UpdateSettingsTabViewModel>();

                            await _dialogManager.ShowDialogAsync(settingsDialog);
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                var errorBoxDialog = _viewModelFactory.CreateMessageBoxViewModel(
                    title: "Some error has occurred",
                    message: $@"
An error has occurred, the error text is shown below:
{exception.Message}".Trim(),
                    okButtonText: "OK",
                    cancelButtonText: null
                );

                await _dialogManager.ShowDialogAsync(errorBoxDialog);
            }

            if (!CheckRole.IsUserAdministrator())
            {
                if (_settingsService.Settings.IsRunAsAdministrator)
                    RestartAsAdmin();
                else
                    await ShowAdminPrivilegesRequirement();
            }

            _updateHardwareMonitorTimer.Start();
        }
    }

    public async void ShowSettings()
    {
        _settingsService.Load();
        await _dialogManager.ShowDialogAsync(_viewModelFactory.CreateSettingsViewModel());
    }

    public void ShowAbout() => OpenUrl.Open(App.GitHubProjectUrl);

    private void Exit() => Application.Current.Shutdown();
}