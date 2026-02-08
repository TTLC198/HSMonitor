using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using HSMonitor.Properties;
using HSMonitor.Services;
using HSMonitor.Services.OtaUpdateService;
using NetSparkleUpdater;
using NetSparkleUpdater.Enums;

namespace HSMonitor.ViewModels.Settings;

#pragma warning disable CA1416
public class UpdateSettingsTabViewModel : SettingsTabBaseViewModel, INotifyPropertyChanged
{
    private readonly UpdateService _appUpdateService;
    private readonly OtaUpdateService _deviceUpdateService;

    public int UpdateAppDownloadPercent
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(AppPrimaryActionText));
            OnPropertyChanged(nameof(IsAppPrimaryActionEnabled));
        }
    }
    
    public int UpdateDeviceDownloadPercent
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(DevicePrimaryActionText));
            OnPropertyChanged(nameof(IsDevicePrimaryActionEnabled));
        }
    }
    
    public int UpdateDeviceUploadPercent
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(DevicePrimaryActionText));
            OnPropertyChanged(nameof(IsDevicePrimaryActionEnabled));
        }
    }

    public bool IsAppProgressBarActive
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(AppPrimaryActionText));
            OnPropertyChanged(nameof(IsAppPrimaryActionEnabled));
            OnPropertyChanged(nameof(AppProgressStageText));
        }
    }
    
    public bool IsDeviceDownloadProgressBarActive
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(DevicePrimaryActionText));
            OnPropertyChanged(nameof(IsDevicePrimaryActionEnabled));
            OnPropertyChanged(nameof(DeviceProgressStageText));
        }
    }
    
    public bool IsDeviceUploadProgressBarActive
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(DevicePrimaryActionText));
            OnPropertyChanged(nameof(IsDevicePrimaryActionEnabled));
            OnPropertyChanged(nameof(DeviceProgressStageText));
        }
    }
    
    public string AppPrimaryActionText =>
        AppUpdateStatus is UpdateStatus.UpdateAvailable or UpdateStatus.UserSkipped
            ? "Обновить"
            : "Проверить";

    public bool IsAppPrimaryActionEnabled =>
        !IsAppProgressBarActive && (AppUpdateStatus is UpdateStatus.UpdateAvailable or UpdateStatus.UserSkipped);

    public string AppProgressStageText =>
        IsAppProgressBarActive ? Resources.DownloadingText : string.Empty;

    public string DevicePrimaryActionText =>
        DeviceUpdateStatus is UpdateStatus.UpdateAvailable or UpdateStatus.UserSkipped
            ? "Обновить прошивку"
            : "Проверить";

    public bool IsDevicePrimaryActionEnabled =>
        !IsDeviceDownloadProgressBarActive && (DeviceUpdateStatus is UpdateStatus.UpdateAvailable or UpdateStatus.UserSkipped);

    public string DeviceProgressStageText =>
        IsDeviceDownloadProgressBarActive ? Resources.DownloadingText : string.Empty;

    public string DevicePrimaryActionTooltip =>
        DeviceUpdateStatus is UpdateStatus.UpdateAvailable or UpdateStatus.UserSkipped
            ? "Скачать и установить новую прошивку"
            : "Сначала нажмите «Проверить», чтобы найти обновления";

    public UpdateStatus AppUpdateStatus
    {
        get;
        set
        {
            field = value;
            AppStatusString =
                value switch
                {
                    UpdateStatus.UpdateAvailable or UpdateStatus.UserSkipped => Resources.UpdateAvailableText,
                    UpdateStatus.UpdateNotAvailable => Resources.UpToDateText,
                    _ => Resources.NoConnectionText
                };
            AppVersionString =
                value switch
                {
                    UpdateStatus.UpdateAvailable or UpdateStatus.UserSkipped => _appUpdateService.GetVersions().First().Version,
                    _ => App.VersionString
                } ?? "";
            OnPropertyChanged();
            OnPropertyChanged(nameof(AppPrimaryActionText));
            OnPropertyChanged(nameof(IsAppPrimaryActionEnabled));
        }
    } = UpdateStatus.CouldNotDetermine;
    
    public UpdateStatus DeviceUpdateStatus
    {
        get;
        set
        {
            field = value;
            DeviceStatusString =
                value switch
                {
                    UpdateStatus.UpdateAvailable or UpdateStatus.UserSkipped => Resources.UpdateAvailableText,
                    UpdateStatus.UpdateNotAvailable => Resources.UpToDateText,
                    _ => Resources.NoConnectionText
                };
            DeviceVersionString =
                value switch
                {
                    UpdateStatus.UpdateAvailable or UpdateStatus.UserSkipped => _appUpdateService.GetVersions().First().Version,
                    _ => App.VersionString
                } ?? "";
            OnPropertyChanged();
            OnPropertyChanged(nameof(DevicePrimaryActionText));
            OnPropertyChanged(nameof(IsDevicePrimaryActionEnabled));
            OnPropertyChanged(nameof(DevicePrimaryActionTooltip));
        }
    } = UpdateStatus.CouldNotDetermine;

    public string AppStatusString
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged();
        }
    } = null!;
    
    public string DeviceStatusString
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged();
        }
    } = null!;

    public string? AppVersionString
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged();
        }
    } = null!;
    
    public string? DeviceVersionString
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged();
        }
    } = null!;

    public async Task CheckAppUpdates()
    {
        await _appUpdateService.CheckForUpdates();
        AppUpdateStatus = _appUpdateService.UpdateStatus;

        // Force status text when nothing is available
        if (AppUpdateStatus is UpdateStatus.UpdateNotAvailable)
            AppStatusString = Resources.UpToDateText;
        else if (AppUpdateStatus is UpdateStatus.CouldNotDetermine)
            AppStatusString = Resources.NoConnectionText;

        AppVersionString = App.VersionString;
    }

    public async Task AppPrimaryAction()
    {
        if (!IsAppPrimaryActionEnabled)
            return;

        IsAppProgressBarActive = true;
        AppStatusString = Resources.DownloadingText;
        await _appUpdateService.UpdateAsync();
    }

    public async Task CheckDeviceUpdates()
    {
        var versions = _deviceUpdateService.GetVersions();
        await _deviceUpdateService.CheckForUpdates();
        DeviceUpdateStatus = _deviceUpdateService.UpdateStatus;

        if (DeviceUpdateStatus is UpdateStatus.UpdateNotAvailable)
            DeviceStatusString = Resources.UpToDateText;
        else if (DeviceUpdateStatus is UpdateStatus.CouldNotDetermine)
            DeviceStatusString = Resources.NoConnectionText;
    }

    public Task DevicePrimaryAction()
    {
        if (!IsDevicePrimaryActionEnabled)
            return Task.CompletedTask;

        IsDeviceDownloadProgressBarActive = true;
        DeviceStatusString = Resources.DownloadingText;

        return _deviceUpdateService.StartDownloadAsync();
    }

    public void GithubLinkPageOpen()
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = App.GitHubProjectUrl,
                UseShellExecute = true
            });
        }
        catch
        {
            // ignore
        }
    }
    
    public async Task AppUpdateHandlerStart()
    {
        switch (AppUpdateStatus)
        {
            case UpdateStatus.UpdateAvailable or UpdateStatus.UserSkipped:
                IsAppProgressBarActive = true;
                AppStatusString = Resources.DownloadingText;
                await _appUpdateService.UpdateAsync();
                break;
            default:
                await _appUpdateService.CheckForUpdates(); 
                AppUpdateStatus = _appUpdateService.UpdateStatus;
                AppStatusString = 
                    AppUpdateStatus switch
                    {
                        UpdateStatus.UpdateNotAvailable => Resources.UpToDateText,
                        _ => Resources.NoConnectionText
                    };
                AppVersionString = App.VersionString;
                break;
        }
    }
    
    public async Task DeviceUpdateHandlerStart()
    {
        switch (DeviceUpdateStatus)
        {
            case UpdateStatus.UpdateAvailable or UpdateStatus.UserSkipped:
                IsDeviceDownloadProgressBarActive = true;
                DeviceStatusString = Resources.DownloadingText;
                await _deviceUpdateService.StartDownloadAsync();
                break;
            default:
                await _deviceUpdateService.CheckForUpdates(); 
                DeviceUpdateStatus = _deviceUpdateService.UpdateStatus;
                DeviceStatusString = 
                    DeviceUpdateStatus switch
                    {
                        UpdateStatus.UpdateNotAvailable => Resources.UpToDateText,
                        _ => Resources.NoConnectionText
                    };
                DeviceVersionString = "0.2.0"; //todo: get version from device
                break;
        }
    }

    public async void OnViewFullyLoaded()
    {
        await _appUpdateService.CheckForUpdates();
        
        AppUpdateStatus = _appUpdateService.UpdateStatus;
        AppStatusString = 
            AppUpdateStatus switch
            {
                UpdateStatus.UpdateAvailable or UpdateStatus.UserSkipped => Resources.UpdateAvailableText,
                UpdateStatus.UpdateNotAvailable => Resources.UpToDateText,
                _ => Resources.NoConnectionText
            };
        AppVersionString = 
            AppUpdateStatus switch
            {
                UpdateStatus.UpdateAvailable or UpdateStatus.UserSkipped => _appUpdateService.GetVersions().Any() 
                    ? $"v{_appUpdateService.GetVersions().First().Version}"
                    : "v...",
                _ => "v..."
            } ?? "";
        
        await _deviceUpdateService.CheckForUpdates();
        DeviceUpdateStatus = _deviceUpdateService.UpdateStatus;
        DeviceStatusString =
            DeviceUpdateStatus switch
            {
                UpdateStatus.UpdateAvailable or UpdateStatus.UserSkipped => Resources.UpdateAvailableText,
                UpdateStatus.UpdateNotAvailable => Resources.UpToDateText,
                _ => Resources.NoConnectionText
            };
    }
    
    private void AppUpdateServiceOnDownloadErrorEvent(AppCastItem item, string? path, Exception exception)
    {
        IsAppProgressBarActive = false;
        AppUpdateStatus = UpdateStatus.CouldNotDetermine;
        AppStatusString = $"{exception.Message}"; //todo: локализация
    }
    
    private void AppUpdateServiceOnDownloadCancelledEvent(AppCastItem item, string path)
    {
        IsAppProgressBarActive = false;
        AppUpdateStatus = UpdateStatus.UserSkipped;
        AppStatusString = $"Скачивание отменено."; //todo: локализация
    }

    public UpdateSettingsTabViewModel(SettingsService settingsService, UpdateService appUpdateService, OtaUpdateService otaUpdateService) 
        : base(settingsService, 4, "Update")
    {
        _appUpdateService = appUpdateService;
        _appUpdateService.UpdateDownloadProcessEvent += (_, args) => UpdateAppDownloadPercent = args.ProgressPercentage;
        _appUpdateService.UpdateDownloadFinishedEvent += (_, _) => IsAppProgressBarActive = false;
        _appUpdateService.DownloadCancelledEvent += AppUpdateServiceOnDownloadCancelledEvent;
        _appUpdateService.DownloadErrorEvent += AppUpdateServiceOnDownloadErrorEvent;

        _deviceUpdateService = otaUpdateService;
        _deviceUpdateService.DownloadProgressFlow.Subscribe(downloadProgress =>
        {
            UpdateDeviceDownloadPercent = downloadProgress.ProgressPercentage;
        });
        _deviceUpdateService.UploadProgressFlow.Subscribe(downloadProgress =>
        {
            UpdateDeviceUploadPercent = downloadProgress.ProgressPercentage;
        });
        _deviceUpdateService.DownloadFinishedFlow.Subscribe(_ =>
        {
            IsDeviceUploadProgressBarActive = true;
            IsDeviceDownloadProgressBarActive = false;
            DeviceStatusString = "Прошивка..."; //todo: локализация
        });
    }

    public new event PropertyChangedEventHandler? PropertyChanged;

    protected override void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}