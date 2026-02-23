using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Ports;
using System.Reactive.Subjects;
using HSMonitor.Models;
using HSMonitor.Properties;
using HSMonitor.Services.OtaUpdateService.Parts;
using HSMonitor.Services.SerialDataService.Parts;
using HSMonitor.Utils.Logger;
using HSMonitor.ViewModels;
using HSMonitor.ViewModels.Framework;
using NetSparkleUpdater;
using NetSparkleUpdater.Configurations;
using NetSparkleUpdater.Enums;
using NetSparkleUpdater.Events;
using NetSparkleUpdater.SignatureVerifiers;

namespace HSMonitor.Services.OtaUpdateService;

[SuppressMessage("Interoperability", "CA1416:Проверка совместимости платформы")]
public class OtaUpdateService
{
  private const int Chunk = 1024;

  private readonly IViewModelFactory _viewModelFactory;
  private readonly DialogManager _dialogManager;
  private readonly SparkleUpdater _updater;
  private readonly ILogger<OtaUpdateService> _logger;

  private readonly Subject<DownloadPercentageEvent> _downloadProgressFlowSubject = new();
  private readonly Subject<DownloadPercentageEvent> _uploadProgressFlowSubject = new();
  private readonly Subject<DownloadFinishedEvent> _downloadFinishedFlowSubject = new();
  private readonly Subject<DownloadHadErrorEvent> _downloadHadErrorFlowSubject = new();
  private readonly Subject<DownloadHadErrorEvent> _uploadHadErrorFlowSubject = new();
  private readonly Subject<UploadFinishedEvent> _uploadFinishedFlowSubject = new();

  private readonly SemaphoreSlim _otaLock = new(1, 1);

  private UpdateInfo? _updateInfo;

  public OtaUpdateService(ILogger<OtaUpdateService> logger, IViewModelFactory viewModelFactory, DialogManager dialogManager)
  {
    _logger = logger;
    _viewModelFactory = viewModelFactory;
    _dialogManager = dialogManager;
    _updater = new SparkleUpdater(App.DeviceAutoUpdateConfigUrl,
      new Ed25519Checker(SecurityMode.Unsafe))
    {
      UserInteractionMode = UserInteractionMode.DownloadNoInstall,
      TmpDownloadFilePath = null,
      RelaunchAfterUpdate = false,
      CustomInstallerArguments = null,
      ClearOldInstallers = null,
      UIFactory = null,
      Configuration = new JSONConfiguration(new ManualAssemblyAccessor(GetProjectVersion)),
      RestartExecutablePath = null!,
      RestartExecutableName = null!,
      RelaunchAfterUpdateCommandPrefix = null,
      UseNotificationToast = false,
      LogWriter = null,
      CheckServerFileName = false,
      UpdateDownloader = null!,
      AppCastDataDownloader = null,
    };
  }

  public UpdateStatus UpdateStatus =>
    _updateInfo?.Status ?? UpdateStatus.CouldNotDetermine;

  public IObservable<DownloadPercentageEvent> DownloadProgressFlow => _downloadProgressFlowSubject;
  public IObservable<DownloadPercentageEvent> UploadProgressFlow => _uploadProgressFlowSubject;
  public IObservable<DownloadFinishedEvent> DownloadFinishedFlow => _downloadFinishedFlowSubject;
  public IObservable<DownloadHadErrorEvent> DownloadHadErrorFlow => _downloadHadErrorFlowSubject;
  public IObservable<DownloadHadErrorEvent> UploadHadErrorFlow => _uploadHadErrorFlowSubject;
  public IObservable<UploadFinishedEvent> UploadFinishedFlow => _uploadFinishedFlowSubject;

  public async Task<UpdateInfo> CheckForUpdates() =>
    _updateInfo = await _updater.CheckForUpdatesQuietly();

  private void UpdaterOnDownloadMadeProgress(ItemDownloadProgressEventArgs args) =>
    _downloadProgressFlowSubject.OnNext(new DownloadPercentageEvent(args.BytesReceived, args.TotalBytesToReceive));

  private void UpdaterOnDownloadHadError(AppCastItem item, string? path, Exception exception) =>
    _downloadHadErrorFlowSubject.OnNext(new DownloadHadErrorEvent(item, path, exception));

  public string GetProjectVersion()
  {
    _otaLock.Wait();
    var version = "";

    try
    {
      version = GetProjectVersionInternal();
    }
    catch (Exception exception)
    {
      _logger.Error(exception);
    }
    finally
    {
      _otaLock.Release();
    }

    return version;
  }
  
  public async Task<string> GetProjectVersionAsync()
  {
    await _otaLock.WaitAsync().ConfigureAwait(false);
    var version = "";

    try
    {
      version = GetProjectVersionInternal();
    }
    catch (Exception exception)
    {
      _logger.Error(exception);
    }
    finally
    {
      _otaLock.Release();
    }

    return version;
  }

  private string GetProjectVersionInternal()
  {
    var version = "";
    var serialPortName = GetDeviceInfo();

    if (serialPortName?.PortName is null)
    {
      _logger.Warn("Устройство для обновления не найдено!");
      return string.Empty;
    }

    using var sp = OpenPort(serialPortName.PortName);

    sp.Open();

    sp.DiscardInBuffer();
    sp.DiscardOutBuffer();
    Thread.Sleep(200);

    var client = new UsbOtaClient(sp);

    version = client.GetProjectVersion();

    if (string.IsNullOrEmpty(version))
    {
      _logger.Warn($"Не удалось определить версию устройства! Полученная версия: [{version}]");
      return string.Empty;
    }
    
    return version;
  }

  public async Task StartDownloadAsync()
  {
    try
    {
      if (_updateInfo is null || _updateInfo.Updates.Count <= 0) throw new InvalidOperationException("UpdateInfo is null");
      _updater.DownloadMadeProgress += (_, _, args) => UpdaterOnDownloadMadeProgress(args);
      _updater.DownloadFinished += async (item, path) => await UpdaterOnDownloadFinished(item, path);
      _updater.DownloadHadError += UpdaterOnDownloadHadError;
      await _updater.InitAndBeginDownload(_updateInfo.Updates.First());
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
  }

  private async Task UpdaterOnDownloadFinished(AppCastItem item, string path)
  {
    try
    {
      if (_updateInfo is null || _updateInfo.Updates.Count <= 0) throw new InvalidOperationException("UpdateInfo is null");
      _downloadFinishedFlowSubject.OnNext(new DownloadFinishedEvent());

      await Task.Run(async () =>
      {
        var file = new FileInfo(path);
        await StartUploadAsync(item, file);
      });
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
  }

  private async Task StartUploadAsync(AppCastItem item, FileInfo file)
  {
    await _otaLock.WaitAsync().ConfigureAwait(false);
    
    try
    {
      if (!file.Exists)
      {
        _logger.Warn("Файл обновления не найден!");

        _uploadHadErrorFlowSubject.OnNext(
          new DownloadHadErrorEvent(
            item,
            file.ToString(),
            new InvalidOperationException("Файл обновления не найден!")));
        return;
      }

      await using var fileDataStream = file.OpenRead();
      var fileDataBuffer = new byte[fileDataStream.Length];

      await fileDataStream.ReadExactlyAsync(fileDataBuffer, 0, fileDataBuffer.Length);

      if (fileDataBuffer.Length == 0)
      {
        _logger.Warn("Поток данных пустой.");
        _uploadHadErrorFlowSubject.OnNext(
          new DownloadHadErrorEvent(
            item,
            file.ToString(),
            new InvalidOperationException("Поток данных пустой.")));
        return;
      }

      var serialPortName = GetDeviceInfo();
      if (serialPortName is null)
      {
        _logger.Warn("Устройство для обновления не найдено!");
        _uploadHadErrorFlowSubject.OnNext(
          new DownloadHadErrorEvent(
            item,
            file.ToString(),
            new InvalidOperationException("Устройство для обновления не найдено!")));
        return;
      }

      SendOtaUpdate(fileDataBuffer, serialPortName.PortName!);
    }
    catch (Exception exception)
    {
      _logger.Error(exception);
      _uploadHadErrorFlowSubject.OnNext(new DownloadHadErrorEvent(item, file.ToString(), exception));
    }
    finally
    {
      _otaLock.Release();
    }
  }

  private DeviceInfo? GetDeviceInfo() =>
    Serial.GetPorts().ToList().FirstOrDefault(p => p.IsHsMonitorOta);

  private SerialPort OpenPort(string portName)
  {
    var sp = new SerialPort(portName, 115200, Parity.None, 8, StopBits.One);
    sp.ReadTimeout = 5000;
    sp.WriteTimeout = 5000;
    sp.DtrEnable = true;
    sp.RtsEnable = true;

    return sp;
  }

  private void SendOtaUpdate(byte[] data, string portName)
  {
    var crc = Crc32.Compute(data);

    using var sp = OpenPort(portName);

    sp.Open();

    sp.DiscardInBuffer();
    sp.DiscardOutBuffer();
    Thread.Sleep(200);

    var client = new UsbOtaClient(sp);

    client.Hello();

    client.Begin((uint) data.Length, crc);

    var offset = 0;
    uint seq = 10;
    var started = DateTime.UtcNow;

    while (offset < data.Length)
    {
      var n = Math.Min(Chunk, data.Length - offset);
      client.Data(seq++, (uint) offset, data.AsSpan(offset, n));
      offset += n;

      if (seq % 50 == 0)
      {
        var progressRecord = new DownloadPercentageEvent(offset, data.Length);
        _logger.Info($"{progressRecord.ProgressPercentage}% ({offset}/{data.Length})");
        _uploadProgressFlowSubject.OnNext(progressRecord);
      }
    }

    client.End(seq);

    _uploadFinishedFlowSubject.OnNext(new UploadFinishedEvent());

    var elapsed = DateTime.UtcNow - started;
    _logger.Info($"Done. Sent {data.Length} bytes in {elapsed.TotalSeconds:0.0}s");
    _logger.Info("Device should reboot into the new firmware.");
  }
}
