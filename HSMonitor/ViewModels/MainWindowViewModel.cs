using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using HSMonitor.Services;
using HSMonitor.Utils;
using HSMonitor.ViewModels.Framework;
using Stylet;

namespace HSMonitor.ViewModels;

public class MainWindowViewModel : Screen
{
    private readonly IViewModelFactory _viewModelFactory;
    private readonly DialogManager _dialogManager;
    private readonly DispatcherTimer _updateHardwareMonitorTimer;

    public DashboardViewModel Dashboard { get; }

    public MainWindowViewModel(
        IViewModelFactory viewModelFactory,
        DialogManager dialogManager,
        HardwareMonitorService hardwareMonitorService,
        SettingsService settingsService,
        SerialMonitorService serialMonitorService)
    {
        _viewModelFactory = viewModelFactory;
        _dialogManager = dialogManager;

        _updateHardwareMonitorTimer = new DispatcherTimer(
            priority: DispatcherPriority.Background,
            interval: TimeSpan.FromMilliseconds(settingsService.Settings.SendInterval == 0 ? 500 : settingsService.Settings.SendInterval),
            callback: (sender, args) =>
            {
                hardwareMonitorService.HardwareInformationUpdate();
                serialMonitorService.SendInformationToMonitor();
            },
            dispatcher: Dispatcher.FromThread(Thread.CurrentThread) ?? throw new InvalidOperationException()
        );

        Dashboard = viewModelFactory.CreateDashboardViewModel();
        DisplayName = $"{App.Name} v{App.VersionString}";
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
        ProcessStartInfo startInfo = new ProcessStartInfo();
        startInfo.UseShellExecute = true;
        startInfo.WorkingDirectory = Environment.CurrentDirectory;
        startInfo.FileName = App.ExecutableFilePath;
        startInfo.Arguments = "restart";

        startInfo.Verb = "runas";
        try
        {
            Process.Start(startInfo);
            Exit();
        }
        catch (Exception ex)
        {
            // UAC elevation failed
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
            await ShowAdminPrivilegesRequirement();
    }

    public async void ShowSettings() =>
        await _dialogManager.ShowDialogAsync(_viewModelFactory.CreateSettingsViewModel());

    public void ShowAbout() => OpenUrl.Open(App.GitHubProjectUrl);

    private void Exit() => Application.Current.Shutdown();
}