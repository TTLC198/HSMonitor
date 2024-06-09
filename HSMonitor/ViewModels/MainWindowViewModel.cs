using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using HSMonitor.Properties;
using HSMonitor.Services;
using HSMonitor.Utils;
using HSMonitor.Utils.Logger;
using HSMonitor.ViewModels.Framework;
using HSMonitor.ViewModels.Settings;
using NetSparkleUpdater.Enums;
using Stylet;

namespace HSMonitor.ViewModels;

#pragma warning disable CA1416
public class MainWindowViewModel : Screen
{
    private readonly DialogManager _dialogManager;
    private readonly HardwareMonitorService _hardwareMonitorService;
    private readonly ILogger<MainWindowViewModel> _logger;
    private readonly SerialMonitorService _serialMonitorService;
    private readonly SettingsService _settingsService;
    private readonly UpdateService _updateService;
    private readonly IViewModelFactory _viewModelFactory;

    private bool _isConnectionErrorWindowOpened;

    private bool _isSerialMonitorEnabled;

    public MainWindowViewModel(
        IViewModelFactory viewModelFactory,
        DialogManager dialogManager,
        HardwareMonitorService hardwareMonitorService,
        SettingsService settingsService,
        SerialMonitorService serialMonitorService,
        UpdateService updateService,
        ILogger<MainWindowViewModel> logger)
    {
        _viewModelFactory = viewModelFactory;
        _dialogManager = dialogManager;
        _hardwareMonitorService = hardwareMonitorService;
        _settingsService = settingsService;
        _serialMonitorService = serialMonitorService;
        _updateService = updateService;
        _logger = logger;

        Dashboard = viewModelFactory.CreateDashboardViewModel();
        DisplayName = $"{App.Name} v{App.VersionString}";

        var culture = new CultureInfo(_settingsService.Settings.ApplicationCultureInfo ?? "en");

        LocalizationManager.ChangeCurrentCulture(culture);
    }

    public DashboardViewModel Dashboard { get; }

    public bool IsSerialMonitorEnabled
    {
        get => _isSerialMonitorEnabled;
        set
        {
            _isSerialMonitorEnabled = value;
            OnPropertyChanged(nameof(IsSerialMonitorEnabled));
        }
    }

    private async void SerialMonitorServiceOnOpenPortAttemptFailed(object? sender, EventArgs e)
    {
        IsSerialMonitorEnabled = false;
        _serialMonitorService.OpenPortAttemptFailed -= SerialMonitorServiceOnOpenPortAttemptFailed;
        _serialMonitorService.OpenPortAttemptSuccessful += SerialMonitorServiceOnOpenPortAttemptSuccessful;

        if (_isConnectionErrorWindowOpened)
            return;

        try
        {
            var messageBoxDialog = _viewModelFactory.CreateMessageBoxViewModel(
                $"{_settingsService.Settings.LastSelectedPort} {Resources.NoConnectionBusyMessageText}",
                Resources.NoConnectionErrorMessageText,
                Resources.MessageBoxOkButtonText,
                Resources.MessageBoxCancelButtonText
            );

            _isConnectionErrorWindowOpened = true;
            var dialogResult = await _dialogManager.ShowDialogAsync(messageBoxDialog);
            if (dialogResult == true)
            {
                var settingsDialog = _viewModelFactory.CreateSettingsViewModel();
                settingsDialog.ActivateTabByType<ConnectionSettingsTabViewModel>();

                await _dialogManager.ShowDialogAsync(settingsDialog);
            }

            _isConnectionErrorWindowOpened = dialogResult is null;
        }
        catch
        {
            _isConnectionErrorWindowOpened = false;
        }
    }

    private void SerialMonitorServiceOnOpenPortAttemptSuccessful(object? sender, EventArgs e)
    {
        if (IsSerialMonitorEnabled)
            return;
        IsSerialMonitorEnabled = true;
        _serialMonitorService.OpenPortAttemptFailed += SerialMonitorServiceOnOpenPortAttemptFailed;
        _serialMonitorService.OpenPortAttemptSuccessful -= SerialMonitorServiceOnOpenPortAttemptSuccessful;
    }

    private async Task ShowAdminPrivilegesRequirement()
    {
        var messageBoxDialog = _viewModelFactory.CreateMessageBoxViewModel(
            Resources.AdminPrivilegesRequirementMessageTitle,
            Resources.AdminPrivilegesRequirementMessageText.Trim(),
            Resources.MessageBoxOkButtonText,
            Resources.MessageBoxCancelButtonText
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
            _logger.Error(exception);

            var messageBoxDialog = _viewModelFactory.CreateMessageBoxViewModel(
                Resources.MessageBoxErrorTitle,
                $@"
{Resources.MessageBoxErrorText}
{exception.Message.Split('\'').Last()}".Trim(),
                Resources.MessageBoxOkButtonText,
                null
            );
            _dialogManager.ShowDialogAsync(messageBoxDialog).GetAwaiter();
        }
    }

    public async void OnViewFullyLoaded()
    {
        if (!File.Exists(_settingsService.ConfigurationPath) || _settingsService is {Settings: null})
        {
            var messageBoxDialog = _viewModelFactory.CreateMessageBoxViewModel(
                Resources.MessageBoxErrorTitle,
                Resources.ConfigurationFileErrorMessageText.Trim(),
                Resources.MessageBoxOkButtonText,
                null
            );

            if (await _dialogManager.ShowDialogAsync(messageBoxDialog) == true)
                Exit();
        }
        else
        {
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
                            Resources.NewUpdateMessageTitle,
                            $@"
{Resources.NewUpdateMessageVersionText} {updateInfo.Updates.First().Version}.         
{Resources.NewUpdateMessageCurrentVersionText} {App.Version.ToString(3).Trim()}.
{Resources.NewUpdateMessageUpdateText}".Trim(),
                            Resources.MessageBoxOkButtonText,
                            Resources.MessageBoxCancelButtonText
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
                _logger.Error(exception);

                var errorBoxDialog = _viewModelFactory.CreateMessageBoxViewModel(
                    Resources.MessageBoxErrorTitle,
                    $@"
{Resources.MessageBoxErrorText}
{exception.Message}".Trim(),
                    Resources.MessageBoxOkButtonText,
                    null
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

            await _hardwareMonitorService.Start();
        }
    }

    public async void ShowSettings()
    {
        await _settingsService.LoadAsync();
        await _dialogManager.ShowDialogAsync(_viewModelFactory.CreateSettingsViewModel());
    }

    public void ShowAbout()
    {
        OpenUrl.Open(App.GitHubProjectUrl);
    }

    private void Exit()
    {
        Application.Current.Shutdown();
    }
}