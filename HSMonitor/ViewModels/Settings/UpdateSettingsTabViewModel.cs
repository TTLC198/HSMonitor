using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using HSMonitor.Properties;
using HSMonitor.Services;
using HSMonitor.Services.HardwareMonitorService;
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
  private readonly HardwareMonitorServiceImpl _hardwareMonitorServiceImpl;
  private readonly ILogger<UpdateSettingsTabViewModel> _logger;

  public bool IsAppInfoLoading
  {
    get;
    set
    {
      field = value;
      OnPropertyChanged();
      OnPropertyChanged(nameof(IsAppPrimaryActionEnabled));
      OnPropertyChanged(nameof(AppPrimaryActionText));
    }
  }

  public bool IsDeviceInfoLoading
  {
    get;
    set
    {
      field = value;
      OnPropertyChanged();
      OnPropertyChanged(nameof(IsDevicePrimaryActionEnabled));
      OnPropertyChanged(nameof(DevicePrimaryActionText));
      OnPropertyChanged(nameof(DevicePrimaryActionTooltip));
    }
  }

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
      OnPropertyChanged(nameof(DeviceDownloadProgressStageText));
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
      OnPropertyChanged(nameof(DeviceUploadProgressStageText));
    }
  }

  public string AppPrimaryActionText =>
    AppUpdateStatus is UpdateStatus.UpdateAvailable or UpdateStatus.UserSkipped
      ? "Обновить"
      : "Проверить";

  public bool IsAppPrimaryActionEnabled =>
    !IsAppInfoLoading &&
    !IsAppProgressBarActive &&
    (AppUpdateStatus is UpdateStatus.UpdateAvailable or UpdateStatus.UserSkipped);

  public string AppProgressStageText =>
    IsAppProgressBarActive ? Resources.DownloadingText : string.Empty;

  public string DevicePrimaryActionText =>
    DeviceUpdateStatus is UpdateStatus.UpdateAvailable or UpdateStatus.UserSkipped
      ? "Обновить прошивку"
      : "Проверить";

  public bool IsDevicePrimaryActionEnabled =>
    !IsDeviceInfoLoading &&
    !IsDeviceDownloadProgressBarActive &&
    (DeviceUpdateStatus is UpdateStatus.UpdateAvailable or UpdateStatus.UserSkipped);

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
    IsAppInfoLoading = true;
    AppStatusString = "Обновление...";
    AppVersionString = "—";

    try
    {
      var updateInfo = await _appUpdateService.CheckForUpdates();
      AppUpdateStatus = _appUpdateService.UpdateStatus;

      AppVersionString =
        updateInfo.Status switch
        {
          UpdateStatus.UpdateAvailable or UpdateStatus.UserSkipped => updateInfo.Updates.First().Version,
          _ => App.VersionString
        } ?? "";
      
      AppStatusString = AppUpdateStatus switch
      {
        UpdateStatus.UpdateNotAvailable => Resources.UpToDateText,
        UpdateStatus.UpdateAvailable => Resources.UpdateAvailableText,
        _ => Resources.NoConnectionText
      };

      OnPropertyChanged(nameof(AppUpdateStatus));
      OnPropertyChanged(nameof(AppStatusString));
      OnPropertyChanged(nameof(AppVersionString));
    }
    finally
    {
      IsAppInfoLoading = false;
    }
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
    IsDeviceInfoLoading = true;
    DeviceStatusString = "Обновление...";
    DeviceVersionString = "—";

    try
    {
      var updateInfo = await _deviceUpdateService.CheckForUpdates();

      var deviceVersion = await _deviceUpdateService.GetProjectVersionAsync();
      var currentDeviceVersionString = string.IsNullOrWhiteSpace(deviceVersion)
        ? "Не удалось получить версию"
        : $"v{deviceVersion}";

      DeviceVersionString =
        updateInfo.Status switch
        {
          UpdateStatus.UpdateAvailable or UpdateStatus.UserSkipped => $"{currentDeviceVersionString} -> v{updateInfo.Updates.FirstOrDefault()?.Version}",
          _ => currentDeviceVersionString
        };

      DeviceUpdateStatus = _deviceUpdateService.UpdateStatus;

      DeviceStatusString = DeviceUpdateStatus switch
      {
        UpdateStatus.UpdateNotAvailable => Resources.UpToDateText,
        UpdateStatus.UpdateAvailable => Resources.UpdateAvailableText,
        _ => Resources.NoConnectionText
      };

      OnPropertyChanged(nameof(DeviceUpdateStatus));
      OnPropertyChanged(nameof(DeviceStatusString));
      OnPropertyChanged(nameof(DeviceVersionString));
    }
    finally
    {
      IsDeviceInfoLoading = false;
    }
  }

  public async Task DevicePrimaryAction()
  {
    if (!IsDevicePrimaryActionEnabled)
      return;

    IsDeviceDownloadProgressBarActive = true;
    DeviceStatusString = "Обновление";

    try
    {
      _hardwareMonitorServiceImpl.Stop();
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
      await Task.WhenAll(CheckAppUpdates(), CheckDeviceUpdates());
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
    AppStatusString = $"{exception.Message}"; // todo: локализация
  }

  private void AppUpdateServiceOnDownloadCancelledEvent(AppCastItem item, string path)
  {
    IsAppProgressBarActive = false;
    AppUpdateStatus = UpdateStatus.UserSkipped;
    AppStatusString = $"Скачивание отменено."; // todo: локализация
  }

  private void DeviceUpdateServiceOnDownloadErrorEvent(DownloadHadErrorEvent errorEvent)
  {
    Execute.OnUIThread(() =>
    {
      IsDeviceDownloadProgressBarActive = false;
      IsDeviceUploadProgressBarActive = false;
      DeviceUpdateStatus = UpdateStatus.CouldNotDetermine;
      DeviceStatusString = $"{errorEvent.Exception.Message}"; // todo: локализация
    });
  }

  public UpdateSettingsTabViewModel(
    SettingsService settingsService,
    UpdateService appUpdateService,
    OtaUpdateService otaUpdateService,
    HardwareMonitorServiceImpl hardwareMonitorServiceImpl,
    ILogger<UpdateSettingsTabViewModel> logger)
    : base(settingsService, 4, "Update")
  {
    _appUpdateService = appUpdateService;
    _deviceUpdateService = otaUpdateService;
    _hardwareMonitorServiceImpl = hardwareMonitorServiceImpl;
    _logger = logger;

    IsAppInfoLoading = true;
    IsDeviceInfoLoading = true;

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
        DeviceStatusString = $"Устройство успешно обновлено!";
      });
      Task.Run(() =>
      {
        _hardwareMonitorServiceImpl.Start();
      });
    });
    _deviceUpdateService.DownloadHadErrorFlow.Subscribe(DeviceUpdateServiceOnDownloadErrorEvent);
    _deviceUpdateService.UploadHadErrorFlow.Subscribe(DeviceUpdateServiceOnDownloadErrorEvent);
  }

  protected override void OnPropertyChanged([CallerMemberName] string? propertyName = null)
  {
    base.OnPropertyChanged(propertyName);
  }
}