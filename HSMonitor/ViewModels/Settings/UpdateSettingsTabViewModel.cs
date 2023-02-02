using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using HSMonitor.Services;
using NetSparkleUpdater;
using NetSparkleUpdater.Enums;
using Stylet;

namespace HSMonitor.ViewModels.Settings;

#pragma warning disable CA1416
public class UpdateSettingsTabViewModel : SettingsTabBaseViewModel, INotifyPropertyChanged
{
    private readonly UpdateService _updateService;

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
                    UpdateStatus.UpdateAvailable or UpdateStatus.UserSkipped => "Update available!",
                    UpdateStatus.UpdateNotAvailable => "You're up to date",
                    _ => "No connection!"
                };
            VersionString = 
                value switch
                {
                    UpdateStatus.UpdateAvailable or UpdateStatus.UserSkipped => _updateService.GetVersions().First().Version,
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
                StatusString = "Downloading...";
                await _updateService.UpdateAsync();
                break;
            default:
                await _updateService.CheckForUpdates();
                UpdateStatus = _updateService.UpdateStatus;
                StatusString = 
                    UpdateStatus switch
                    {
                        UpdateStatus.UpdateNotAvailable => "You're up to date",
                        _ => "No connection"
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
                UpdateStatus.UpdateAvailable or UpdateStatus.UserSkipped => "Update available!",
                UpdateStatus.UpdateNotAvailable => "You're up to date",
                _ => "No connection"
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

    public UpdateSettingsTabViewModel(SettingsService settingsService, UpdateService updateService) 
        : base(settingsService, 4, "Update")
    {
        _updateService = updateService;
        _updateService.UpdateDownloadProcessEvent += (_, args) => UpdateDownloadPercent = args.ProgressPercentage;
        _updateService.UpdateDownloadFinishedEvent += (_, _) => IsProgressBarActive = false;
        _updateService.CheckForUpdates().GetAwaiter();
    }
    
    public new event PropertyChangedEventHandler? PropertyChanged;

    protected override void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}