using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using HSMonitor.Properties;
using HSMonitor.Services;
using NetSparkleUpdater.Enums;

namespace HSMonitor.ViewModels.Settings;

#pragma warning disable CA1416
public class UpdateSettingsTabViewModel : SettingsTabBaseViewModel, INotifyPropertyChanged
{
    private readonly UpdateService _updateService;
    private bool _isProgressBarActive;
    private string _statusString = null!;

    private int _updateDownloadPercent;
    private UpdateStatus _updateStatus = UpdateStatus.CouldNotDetermine;
    private string _versionString = null!;

    public UpdateSettingsTabViewModel(SettingsService settingsService, UpdateService updateService)
        : base(settingsService, 4, "Update")
    {
        _updateService = updateService;
        _updateService.UpdateDownloadProcessEvent += (_, args) => UpdateDownloadPercent = args.ProgressPercentage;
        _updateService.UpdateDownloadFinishedEvent += (_, _) => IsProgressBarActive = false;
        _updateService.CheckForUpdates().GetAwaiter();
    }

    public int UpdateDownloadPercent
    {
        get => _updateDownloadPercent;
        set
        {
            _updateDownloadPercent = value;
            OnPropertyChanged();
        }
    }


    public bool IsProgressBarActive
    {
        get => _isProgressBarActive;
        set
        {
            _isProgressBarActive = value;
            OnPropertyChanged();
        }
    }

    public UpdateStatus UpdateStatus
    {
        get => _updateStatus;
        set
        {
            _updateStatus = value;
            StatusString =
                value switch
                {
                    UpdateStatus.UpdateAvailable or UpdateStatus.UserSkipped => Resources.UpdateAvailableText,
                    UpdateStatus.UpdateNotAvailable => Resources.UpToDateText,
                    _ => Resources.NoConnectionText
                };
            VersionString =
                value switch
                {
                    UpdateStatus.UpdateAvailable or UpdateStatus.UserSkipped => _updateService.GetVersions().First()
                        .Version,
                    _ => App.VersionString
                };
            OnPropertyChanged();
        }
    }

    public string StatusString
    {
        get => _statusString;
        set
        {
            _statusString = value;
            OnPropertyChanged();
        }
    }

    public string VersionString
    {
        get => _versionString;
        set
        {
            _versionString = value;
            OnPropertyChanged();
        }
    }

    public new event PropertyChangedEventHandler? PropertyChanged;

    public async Task UpdateHandlerStart()
    {
        switch (UpdateStatus)
        {
            case UpdateStatus.UpdateAvailable or UpdateStatus.UserSkipped:
                IsProgressBarActive = true;
                StatusString = Resources.DownloadingText;
                await _updateService.UpdateAsync();
                break;
            default:
                await _updateService.CheckForUpdates();
                UpdateStatus = _updateService.UpdateStatus;
                StatusString =
                    UpdateStatus switch
                    {
                        UpdateStatus.UpdateNotAvailable => Resources.UpToDateText,
                        _ => Resources.NoConnectionText
                    };
                VersionString = App.VersionString;
                break;
        }
    }

    public async void OnViewFullyLoaded()
    {
        await _updateService.CheckForUpdates();

        UpdateStatus = _updateService.UpdateStatus;
        StatusString =
            UpdateStatus switch
            {
                UpdateStatus.UpdateAvailable or UpdateStatus.UserSkipped => Resources.UpdateAvailableText,
                UpdateStatus.UpdateNotAvailable => Resources.UpToDateText,
                _ => Resources.NoConnectionText
            };
        VersionString =
            UpdateStatus switch
            {
                UpdateStatus.UpdateAvailable or UpdateStatus.UserSkipped => _updateService.GetVersions().Count() > 0
                    ? $"v{_updateService.GetVersions().First().Version}"
                    : App.VersionString,
                _ => App.VersionString
            };
    }

    protected override void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}