using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using HSMonitor.Properties;
using HSMonitor.Services;
using HSMonitor.Services.OtaUpdateService;
using HSMonitor.Services.OtaUpdateService.Parts;
using HSMonitor.Utils.Logger;
using NetSparkleUpdater;
using NetSparkleUpdater.Enums;
using Stylet;

namespace HSMonitor.ViewModels.Settings;

#pragma warning disable CA1416
public class UpdateSettingsTabViewModel : SettingsTabBaseViewModel, INotifyPropertyChanged
{
  private readonly UpdateService _appUpdateService;
  private readonly OtaUpdateService _deviceUpdateService;
  private readonly HardwareMonitorService _hardwareMonitorService;
  private readonly ILogger<UpdateSettingsTabViewModel> _logger;

  public int UpdateAppDownloadPercent
  {
    get;
    set
    {
      field = value;
      OnPropertyChanged();
      OnPropertyChanged(nameof (AppPrimaryActionText));
      OnPropertyChanged(nameof (IsAppPrimaryActionEnabled));
    }
  }

  public int UpdateDeviceDownloadPercent
  {
    get;
    set
    {
      field = value;
      OnPropertyChanged();
      OnPropertyChanged(nameof (DevicePrimaryActionText));
      OnPropertyChanged(nameof (IsDevicePrimaryActionEnabled));
    }
  }

  public int UpdateDeviceUploadPercent
  {
    get;
    set
    {
      field = value;
      OnPropertyChanged();
      OnPropertyChanged(nameof (DevicePrimaryActionText));
      OnPropertyChanged(nameof (IsDevicePrimaryActionEnabled));
    }
  }

  public bool IsAppProgressBarActive
  {
    get;
    set
    {
      field = value;
      OnPropertyChanged();
      OnPropertyChanged(nameof (AppPrimaryActionText));
      OnPropertyChanged(nameof (IsAppPrimaryActionEnabled));
      OnPropertyChanged(nameof (AppProgressStageText));
    }
  }

  public bool IsDeviceDownloadProgressBarActive
  {
    get;
    set
    {
      field = value;
      OnPropertyChanged();
      OnPropertyChanged(nameof (DevicePrimaryActionText));
      OnPropertyChanged(nameof (IsDevicePrimaryActionEnabled));
      OnPropertyChanged(nameof (DeviceDownloadProgressStageText));
    }
  }

  public bool IsDeviceUploadProgressBarActive
  {
    get;
    set
    {
      field = value;
      OnPropertyChanged();
      OnPropertyChanged(nameof (DevicePrimaryActionText));
      OnPropertyChanged(nameof (IsDevicePrimaryActionEnabled));
      OnPropertyChanged(nameof (DeviceUploadProgressStageText));
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

  public string DeviceDownloadProgressStageText =>
    IsDeviceDownloadProgressBarActive ? Resources.DownloadingText : string.Empty;

  public string DeviceUploadProgressStageText =>
    IsDeviceUploadProgressBarActive ? "Установка..." : string.Empty;

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
      OnPropertyChanged(nameof (AppPrimaryActionText));
      OnPropertyChanged(nameof (IsAppPrimaryActionEnabled));
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

      var deviceVersion = _deviceUpdateService.GetProjectVersionAsync();
      var versionString = string.IsNullOrWhiteSpace(deviceVersion) 
        ? "Не удалось получить версию"
        : $"v{deviceVersion}";
      DeviceVersionString =
        value switch
        {
          UpdateStatus.UpdateAvailable or UpdateStatus.UserSkipped => _appUpdateService.GetVersions().FirstOrDefault()?.Version,
          _ => versionString
        } ?? "";
      OnPropertyChanged();
      OnPropertyChanged(nameof (DevicePrimaryActionText));
      OnPropertyChanged(nameof (IsDevicePrimaryActionEnabled));
      OnPropertyChanged(nameof (DevicePrimaryActionTooltip));
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
    await _deviceUpdateService.CheckForUpdates();
    DeviceUpdateStatus = _deviceUpdateService.UpdateStatus;

    if (DeviceUpdateStatus is UpdateStatus.UpdateNotAvailable)
      DeviceStatusString = Resources.UpToDateText;
    else if (DeviceUpdateStatus is UpdateStatus.CouldNotDetermine)
      DeviceStatusString = Resources.NoConnectionText;
  }

  public async Task DevicePrimaryAction()
  {
    if (!IsDevicePrimaryActionEnabled)
      return;

    IsDeviceDownloadProgressBarActive = true;
    DeviceStatusString = "Обновление";

    try
    {
      _hardwareMonitorService.Stop();
      await _deviceUpdateService.StartDownloadAsync();
    }
    catch (Exception exception)
    {
      _logger.Error(exception);
    }
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

  public async Task OnViewFullyLoaded()
  {
    try
    {
      await Task.Run(async () =>
      {
        await _appUpdateService.CheckForUpdates();
        await _deviceUpdateService.CheckForUpdates();
      });
      
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

      DeviceUpdateStatus = _deviceUpdateService.UpdateStatus;
      DeviceStatusString =
        DeviceUpdateStatus switch
        {
          UpdateStatus.UpdateAvailable or UpdateStatus.UserSkipped => Resources.UpdateAvailableText,
          UpdateStatus.UpdateNotAvailable => Resources.UpToDateText,
          _ => Resources.NoConnectionText
        };
    }
    catch (Exception exception)
    {
      _logger.Error(exception);
    }
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

  private void DeviceUpdateServiceOnDownloadErrorEvent(DownloadHadErrorEvent errorEvent)
  {
    Execute.OnUIThread(() =>
    {
      IsDeviceDownloadProgressBarActive = false;
      IsDeviceUploadProgressBarActive = false;
      DeviceUpdateStatus = UpdateStatus.CouldNotDetermine;
      DeviceStatusString = $"{errorEvent.Exception.Message}"; //todo: локализация
    });
  }

  public UpdateSettingsTabViewModel(SettingsService settingsService, UpdateService appUpdateService, OtaUpdateService otaUpdateService, HardwareMonitorService hardwareMonitorService, ILogger<UpdateSettingsTabViewModel> logger)
    : base(settingsService, 4, "Update")
  {
    _appUpdateService = appUpdateService;
    _deviceUpdateService = otaUpdateService;
    _hardwareMonitorService = hardwareMonitorService;
    _logger = logger;

    _appUpdateService.UpdateDownloadProcessEvent += (_, args) => UpdateAppDownloadPercent = args.ProgressPercentage;
    _appUpdateService.UpdateDownloadFinishedEvent += (_, _) => IsAppProgressBarActive = false;
    _appUpdateService.DownloadCancelledEvent += AppUpdateServiceOnDownloadCancelledEvent;
    _appUpdateService.DownloadErrorEvent += AppUpdateServiceOnDownloadErrorEvent;

    _deviceUpdateService.DownloadProgressFlow.Subscribe(downloadProgress =>
    {
      Execute.OnUIThread(() =>
      {
        UpdateDeviceDownloadPercent = downloadProgress.ProgressPercentage;
        IsDeviceUploadProgressBarActive = false;
        IsDeviceDownloadProgressBarActive = true;
      });
    });
    _deviceUpdateService.UploadProgressFlow.Subscribe(uploadProgress =>
    {
      Execute.OnUIThread(() =>
      {
        UpdateDeviceUploadPercent = uploadProgress.ProgressPercentage;
        IsDeviceUploadProgressBarActive = true;
        IsDeviceDownloadProgressBarActive = false;
      });
    });
    _deviceUpdateService.DownloadFinishedFlow.Subscribe(_ =>
    {
      Execute.OnUIThread(() =>
      {
        UpdateDeviceDownloadPercent = 100;
        IsDeviceUploadProgressBarActive = true;
        IsDeviceDownloadProgressBarActive = false;
      });
    });
    _deviceUpdateService.UploadFinishedFlow.Subscribe(_ =>
    {
      Execute.OnUIThread(() =>
      {
        UpdateDeviceUploadPercent = 100;
        IsDeviceDownloadProgressBarActive = false;
        IsDeviceUploadProgressBarActive = false;
      });
      _hardwareMonitorService.Stop();
    });
    _deviceUpdateService.DownloadHadErrorFlow.Subscribe(DeviceUpdateServiceOnDownloadErrorEvent);
    _deviceUpdateService.UploadHadErrorFlow.Subscribe(DeviceUpdateServiceOnDownloadErrorEvent);
  }

  protected override void OnPropertyChanged([CallerMemberName] string? propertyName = null)
  {
    base.OnPropertyChanged(propertyName);
  }
}
