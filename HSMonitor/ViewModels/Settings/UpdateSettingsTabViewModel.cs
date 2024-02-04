using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using HSMonitor.Properties;
using HSMonitor.Services;
using NetSparkleUpdater;
using NetSparkleUpdater.Enums;
using Stylet;

namespace HSMonitor.ViewModels.Settings;

#pragma warning disable CA1416
public class UpdateSettingsTabViewModel : SettingsTabBaseViewModel, INotifyPropertyChanged
{
    private readonly ProgramUpdateService _programUpdateService;
    private readonly FirmwareUpdateService _firmwareUpdateService;

    private int _updateDownloadPercent;
    private bool _isProgressBarActive;
    private string _statusString = null!;
    private string _versionString = null!;
    private UpdateStatus _updateStatus = UpdateStatus.CouldNotDetermine;

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
                    UpdateStatus.UpdateAvailable or UpdateStatus.UserSkipped => _programUpdateService.GetVersions().First().Version,
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

    public async Task UpdateHandlerStart()
    {
        switch (UpdateStatus)
        {
            case UpdateStatus.UpdateAvailable or UpdateStatus.UserSkipped:
                IsProgressBarActive = true;
                StatusString = Resources.DownloadingText;
                await _programUpdateService.UpdateAsync();
                break;
            default:
                await _programUpdateService.CheckForUpdates();
                UpdateStatus = _programUpdateService.UpdateStatus;
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
    
    public async Task FirmwareUpdateHandlerStart()
    {
        await _firmwareUpdateService.UpdateAsync();
    }

    public async void OnViewFullyLoaded()
    {
        await _programUpdateService.CheckForUpdates();
        
        UpdateStatus = _programUpdateService.UpdateStatus;
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
                UpdateStatus.UpdateAvailable or UpdateStatus.UserSkipped => _programUpdateService.GetVersions().Count() > 0 
                    ? $"v{_programUpdateService.GetVersions().First().Version}"
                    : App.VersionString,
                _ => App.VersionString
            };
    }

    public UpdateSettingsTabViewModel(SettingsService settingsService, ProgramUpdateService programUpdateService, FirmwareUpdateService firmwareUpdateService) 
        : base(settingsService, 4, "Update")
    {
        _programUpdateService = programUpdateService;
        _firmwareUpdateService = firmwareUpdateService;
        _programUpdateService.UpdateDownloadProcessEvent += (_, args) => UpdateDownloadPercent = args.ProgressPercentage;
        _programUpdateService.UpdateDownloadFinishedEvent += (_, _) => IsProgressBarActive = false;
        _programUpdateService.CheckForUpdates().GetAwaiter();
    }
    
    public new event PropertyChangedEventHandler? PropertyChanged;

    protected override void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}