using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using HSMonitor.Services;
using HSMonitor.Utils;
using HSMonitor.ViewModels.Framework;
using HSMonitor.ViewModels.Settings;
using Stylet;

namespace HSMonitor.ViewModels;

public class MainWindowViewModel : Screen
{
    private readonly IViewModelFactory _viewModelFactory;
    private readonly DialogManager _dialogManager;
    private readonly DispatcherTimer _updateHardwareMonitorTimer;
    private readonly SettingsService _settingsService;
    private readonly SerialMonitorService _serialMonitorService;

    public DashboardViewModel Dashboard { get; }

    private bool _isSerialMonitorEnabled = true;
    public bool IsSerialMonitorEnabled
    {
        get => _isSerialMonitorEnabled;
        set
        {
            _isSerialMonitorEnabled = value;
#pragma warning disable CA1416
            OnPropertyChanged(nameof(IsSerialMonitorEnabled));
#pragma warning restore CA1416
        }
    }

    public MainWindowViewModel(
        IViewModelFactory viewModelFactory,
        DialogManager dialogManager,
        HardwareMonitorService hardwareMonitorService,
        SettingsService settingsService,
        SerialMonitorService serialMonitorService)
    {
        _viewModelFactory = viewModelFactory;
        _dialogManager = dialogManager;
        _settingsService = settingsService;
        _serialMonitorService = serialMonitorService;

        _updateHardwareMonitorTimer = new DispatcherTimer(
            priority: DispatcherPriority.Background,
            interval: TimeSpan.FromMilliseconds(settingsService.Settings.SendInterval == 0 ? 500 : settingsService.Settings.SendInterval),
            callback: (_, _) =>
            {
                hardwareMonitorService.HardwareInformationUpdate();
            },
            dispatcher: Dispatcher.FromThread(Thread.CurrentThread) ?? throw new InvalidOperationException()
        );

        _settingsService.SettingsSaved += (_, _) =>
        {
            _updateHardwareMonitorTimer.Interval = TimeSpan.FromMilliseconds(settingsService.Settings.SendInterval == 0 ? 500 : settingsService.Settings.SendInterval);
        };
        
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
            _dialogManager.ShowDialogAsync(messageBoxDialog);
        }
    }

    protected override void OnViewLoaded()
    {
        base.OnViewLoaded();
        _updateHardwareMonitorTimer.Start();
    }
    
    public async void OnViewFullyLoaded()
    {
        if (!CheckRole.IsUserAdministrator())
        {
            if (_settingsService.Settings.IsRunAsAdministrator) 
                RestartAsAdmin();
            else
                await ShowAdminPrivilegesRequirement();
        }
        _serialMonitorService.OpenPortAttemptFailed += async (_,_) => await SerialMonitorServiceOnOpenPortAttemptFailed();
        _serialMonitorService.OpenPortAttemptSuccessful += (_, _) =>
        {
            if (IsSerialMonitorEnabled) 
                return;
            IsSerialMonitorEnabled = true;
        };
    }

    public async void ShowSettings()
    {
        _settingsService.Load();
        await _dialogManager.ShowDialogAsync(_viewModelFactory.CreateSettingsViewModel());
    }

    public void ShowAbout() => OpenUrl.Open(App.GitHubProjectUrl);

    private void Exit() => Application.Current.Shutdown();
}