using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using HSMonitor.Properties;
using HSMonitor.Services;
using HSMonitor.Services.HardwareMonitorService;
using HSMonitor.Services.SerialDataService;
using HSMonitor.Utils;
using HSMonitor.Utils.Logger;
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
    private readonly SerialDataService _serialDataService;
    private readonly HardwareMonitorServiceImpl _hardwareMonitorServiceImpl;
    private readonly UpdateService _updateService;
    private readonly ILogger<MainWindowViewModel> _logger;
    public DashboardViewModel Dashboard { get; }

    private bool _isConnectionErrorWindowOpened;
    
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
        HardwareMonitorServiceImpl hardwareMonitorServiceImpl,
        SettingsService settingsService,
        SerialDataService serialDataService,
        UpdateService updateService, 
        ILogger<MainWindowViewModel> logger)
    {
        _viewModelFactory = viewModelFactory;
        _dialogManager = dialogManager;
        _hardwareMonitorServiceImpl = hardwareMonitorServiceImpl;
        _settingsService = settingsService;
        _serialDataService = serialDataService;
        _updateService = updateService;
        _logger = logger;

        Dashboard = viewModelFactory.CreateDashboardViewModel();
        DisplayName = $"{App.Name} v{App.VersionString}";
        
        var culture = new System.Globalization.CultureInfo(_settingsService.Settings.ApplicationCultureInfo ?? "en");
        
        LocalizationManager.ChangeCurrentCulture(culture);
    }
    
    private async void SerialDataServiceOnOpenPortAttemptFailed(object? sender, EventArgs e)
    {
        try
        {
            IsSerialMonitorEnabled = false;
            _serialDataService.OpenPortAttemptFailed -= SerialDataServiceOnOpenPortAttemptFailed;
            _serialDataService.OpenPortAttemptSuccessful += SerialDataServiceOnOpenPortAttemptSuccessful;
        
            if (_isConnectionErrorWindowOpened)
                return;
        
            try
            {
                var messageBoxDialog = _viewModelFactory.CreateMessageBoxViewModel(
                    title: $"{_settingsService.Settings.LastSelectedPort} {Resources.NoConnectionBusyMessageText}",
                    message: Resources.NoConnectionErrorMessageText,
                    okButtonText: Resources.MessageBoxOkButtonText,
                    cancelButtonText: Resources.MessageBoxCancelButtonText
                );

                _isConnectionErrorWindowOpened = true;
                var dialogResult = await _dialogManager.ShowDialogAsync(messageBoxDialog);
                if (dialogResult)
                {
                    var settingsDialog = _viewModelFactory.CreateSettingsViewModel();
                    settingsDialog.ActivateTabByType<ConnectionSettingsTabViewModel>();

                    await _dialogManager.ShowSettingsDialogAsync(settingsDialog);
                }
                _isConnectionErrorWindowOpened = dialogResult;
            }
            catch 
            {
                _isConnectionErrorWindowOpened = false;
            }
        }
        catch (Exception exception)
        {
            _logger.Error(exception);
        }
    }
    
    private void SerialDataServiceOnOpenPortAttemptSuccessful(object? sender, EventArgs e)
    {
        if (IsSerialMonitorEnabled)
            return;
        IsSerialMonitorEnabled = true;
        _serialDataService.OpenPortAttemptFailed += SerialDataServiceOnOpenPortAttemptFailed;
        _serialDataService.OpenPortAttemptSuccessful -= SerialDataServiceOnOpenPortAttemptSuccessful;
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
        catch (Win32Exception exception)
        {
            /* ignored */
        }
        catch (Exception exception)
        {
            _logger.Error(exception);
            
            var messageBoxDialog = _viewModelFactory.CreateMessageBoxViewModel(
                title: Resources.MessageBoxErrorTitle,
                message: $@"
{Resources.MessageBoxErrorText}
{exception.Message.Split('\'').Last().Replace(".", "").Trim()}".Trim(),
                okButtonText: Resources.MessageBoxOkButtonText,
                cancelButtonText: null
            );
            _dialogManager.ShowDialogAsync(messageBoxDialog).GetAwaiter();
        }
    }

    public async Task OnViewFullyLoaded()
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
            _serialDataService.OpenPortAttemptSuccessful += SerialDataServiceOnOpenPortAttemptSuccessful;

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

                            await _dialogManager.ShowSettingsDialogAsync(settingsDialog);
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                _logger.Error(exception);
                
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

            _hardwareMonitorServiceImpl.Start();
        }
    }

    public async Task ShowSettings()
    {
        await _settingsService.LoadAsync();
        await _dialogManager.ShowSettingsDialogAsync(_viewModelFactory.CreateSettingsViewModel());
    }

    public void ShowAbout() => OpenUrl.Open(App.GitHubProjectUrl);

    private void Exit() => Application.Current.Shutdown();
}