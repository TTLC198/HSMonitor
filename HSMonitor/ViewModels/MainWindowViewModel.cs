using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using HSMonitor.Properties;
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
    
    private bool _isSerialMonitorEnabled = false;

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
        
        var culture = new System.Globalization.CultureInfo(_settingsService.Settings.ApplicationCultureInfo ?? "en");
        
        LocalizationManager.ChangeCurrentCulture(culture);
    }
    
    private async void SerialMonitorServiceOnOpenPortAttemptFailed(object? sender, EventArgs e)
    {
        IsSerialMonitorEnabled = false;
        _serialMonitorService.OpenPortAttemptFailed -= SerialMonitorServiceOnOpenPortAttemptFailed;
        _serialMonitorService.OpenPortAttemptSuccessful += SerialMonitorServiceOnOpenPortAttemptSuccessful;
        
        var messageBoxDialog = _viewModelFactory.CreateMessageBoxViewModel(
            title: $"{_settingsService.Settings.LastSelectedPort} {Resources.NoConnectionBusyMessageText}",
            message: Resources.NoConnectionErrorMessageText,
            okButtonText: Resources.MessageBoxOkButtonText,
            cancelButtonText: Resources.MessageBoxCancelButtonText
        );

        if (await _dialogManager.ShowDialogAsync(messageBoxDialog) == true)
        {
            var settingsDialog = _viewModelFactory.CreateSettingsViewModel();
            settingsDialog.ActivateTabByType<ConnectionSettingsTabViewModel>();

            await _dialogManager.ShowDialogAsync(settingsDialog);
        }
    }
    
    private void SerialMonitorServiceOnOpenPortAttemptSuccessful(object? sender, EventArgs e)
    {
        IsSerialMonitorEnabled = true;
        _serialMonitorService.OpenPortAttemptFailed += SerialMonitorServiceOnOpenPortAttemptFailed;
        _serialMonitorService.OpenPortAttemptSuccessful -= SerialMonitorServiceOnOpenPortAttemptSuccessful;
    }

    private async Task ShowAdminPrivilegesRequirement()
    {
        var messageBoxDialog = _viewModelFactory.CreateMessageBoxViewModel(
            title: Resources.AdminPrivilegesRequirementMessageTitle,
            message: Resources.AdminPrivilegesRequirementMessageText.Trim(),
            okButtonText: Resources.MessageBoxOkButtonText,
            cancelButtonText: Resources.MessageBoxCancelButtonText
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
                title: Resources.MessageBoxErrorTitle,
                message: $@"
{Resources.MessageBoxErrorText}
{exception.Message.Split('\'').Last()}".Trim(),
                okButtonText: Resources.MessageBoxOkButtonText,
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
                title: Resources.MessageBoxErrorTitle,
                message: Resources.ConfigurationFileErrorMessageText.Trim(),
                okButtonText: Resources.MessageBoxOkButtonText,
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

            _serialMonitorService.OpenPortAttemptSuccessful += SerialMonitorServiceOnOpenPortAttemptSuccessful;

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
                            title: Resources.NewUpdateMessageTitle,
                            message: $@"
{Resources.NewUpdateMessageVersionText} {updateInfo.Updates.First().Version}.         
{Resources.NewUpdateMessageCurrentVersionText} {App.Version.ToString(3).Trim()}.
{Resources.NewUpdateMessageUpdateText}".Trim(),
                            okButtonText: Resources.MessageBoxOkButtonText,
                            cancelButtonText: Resources.MessageBoxCancelButtonText
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
                    title: Resources.MessageBoxErrorTitle,
                    message: $@"
{Resources.MessageBoxErrorText}
{exception.Message}".Trim(),
                    okButtonText: Resources.MessageBoxOkButtonText,
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
        await _settingsService.Load();
        await _dialogManager.ShowDialogAsync(_viewModelFactory.CreateSettingsViewModel());
    }

    public void ShowAbout() => OpenUrl.Open(App.GitHubProjectUrl);

    private void Exit() => Application.Current.Shutdown();
}