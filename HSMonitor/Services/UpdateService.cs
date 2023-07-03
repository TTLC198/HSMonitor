using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Windows;
using HSMonitor.Properties;
using HSMonitor.ViewModels;
using HSMonitor.ViewModels.Framework;
using NetSparkleUpdater;
using NetSparkleUpdater.Enums;
using NetSparkleUpdater.Events;
using NetSparkleUpdater.SignatureVerifiers;

namespace HSMonitor.Services;

public class UpdateService
{
    private readonly IViewModelFactory _viewModelFactory;
    private readonly DialogManager _dialogManager;
    private readonly SparkleUpdater _updater;

    private UpdateInfo? _updateInfo;
    
    public event DownloadProgressEvent? UpdateDownloadProcessEvent;
    public event DownloadDataCompletedEventHandler? UpdateDownloadFinishedEvent;

    public UpdateStatus UpdateStatus =>
        _updateInfo is not null ? _updateInfo.Status : UpdateStatus.CouldNotDetermine;

    public async Task UpdateAsync()
    {
        try
        {
            if (_updateInfo is null || _updateInfo.Updates.Count <= 0) throw new InvalidOperationException("UpdateInfo is null");
            _updater.DownloadMadeProgress += UpdaterOnDownloadMadeProgress;
            _updater.DownloadFinished += async (item, path) => await UpdaterOnDownloadFinished(item, path);
            await _updater.InitAndBeginDownload(_updateInfo.Updates.First());
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
    }

    private async Task UpdaterOnDownloadFinished(AppCastItem item, string path)
    {
        try
        {
            if (_updateInfo is null || _updateInfo.Updates.Count <= 0) throw new InvalidOperationException("UpdateInfo is null");
            UpdateDownloadFinishedEvent?.Invoke(this, null!);
            _updater.CloseApplication += UpdaterOnCloseApplication;
            
            var restartBoxDialog = _viewModelFactory.CreateMessageBoxViewModel(
                title: Resources.UpdateCompletedTitle,
                message: Resources.UpdateCompletedText.Trim(),
                okButtonText: Resources.MessageBoxRestartButtonText,
                cancelButtonText: Resources.MessageBoxCancelButtonText
            );

            if (await _dialogManager.ShowDialogAsync(restartBoxDialog) == true)
            {
                _updater.InstallUpdate(_updateInfo.Updates.First());
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
    }

    public IEnumerable<AppCastItem> GetVersions() =>
        (_updateInfo ?? CheckForUpdates().GetAwaiter().GetResult())
        .Updates
        .ToList();

    public async Task<UpdateInfo> CheckForUpdates() => 
        _updateInfo = await _updater.CheckForUpdatesQuietly();

    private void UpdaterOnDownloadMadeProgress(object sender, AppCastItem item, ItemDownloadProgressEventArgs args) =>
        UpdateDownloadProcessEvent?.Invoke(this, args);

    private void UpdaterOnCloseApplication() =>
        Application.Current.Shutdown();


    public UpdateService(IViewModelFactory viewModelFactory, DialogManager dialogManager)
    {
        _viewModelFactory = viewModelFactory;
        _dialogManager = dialogManager;
        _updater = new SparkleUpdater(App.GitHubAutoUpdateConfigUrl,
            new Ed25519Checker(SecurityMode.Unsafe))
        {
            SecurityProtocolType = SecurityProtocolType.SystemDefault,
            UserInteractionMode = UserInteractionMode.DownloadNoInstall,
            TmpDownloadFilePath = null,
            RelaunchAfterUpdate = false,
            CustomInstallerArguments = null,
            ClearOldInstallers = null,
            UIFactory = null,
            Configuration = null,
            RestartExecutablePath = null,
            RestartExecutableName = null,
            RelaunchAfterUpdateCommandPrefix = null,
            UseNotificationToast = false,
            ShowsUIOnMainThread = false,
            LogWriter = null,
            CheckServerFileName = false,
            UpdateDownloader = null,
            AppCastDataDownloader = null,
            AppCastHandler = null
        };
    }
}