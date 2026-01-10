using System;
using System.ComponentModel;
using System.Linq;
using System.Reactive;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using HSMonitor.Properties;
using HSMonitor.Services;
using HSMonitor.Services.OtaUpdateService;
using HSMonitor.Services.OtaUpdateService.Parts;
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
        }
    }
    
    public int UpdateDeviceDownloadPercent
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged();
        }
    }


    public bool IsAppProgressBarActive
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged();
        }
    }
    
    public bool IsDeviceProgressBarActive
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged();
        }
    }

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
                IsAppProgressBarActive = true;
                AppStatusString = Resources.DownloadingText;
                _deviceUpdateService.SendOtaUpdate();
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
                    : App.VersionString,
                _ => App.VersionString
            } ?? "";
    }

    public UpdateSettingsTabViewModel(SettingsService settingsService, UpdateService appUpdateService, OtaUpdateService otaUpdateService) 
        : base(settingsService, 4, "Update")
    {
        _appUpdateService = appUpdateService;
        _appUpdateService.UpdateDownloadProcessEvent += (_, args) => UpdateAppDownloadPercent = args.ProgressPercentage;
        _appUpdateService.UpdateDownloadFinishedEvent += (_, _) => IsAppProgressBarActive = false;
        _appUpdateService.CheckForUpdates().GetAwaiter();

        _deviceUpdateService = otaUpdateService;
        _deviceUpdateService.DownloadProgress.Subscribe(downloadProgress =>
        {
            UpdateDeviceDownloadPercent = downloadProgress.ProgressPercentage;
        });
        _deviceUpdateService.DownloadFinished.Subscribe(_ =>
        {
            IsDeviceProgressBarActive = false;
        });
    }
    
    public new event PropertyChangedEventHandler? PropertyChanged;

    protected override void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}