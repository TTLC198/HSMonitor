using System;
using System.Diagnostics.CodeAnalysis;
using System.IO.Ports;
using System.Reactive.Subjects;
using System.Threading;
using HSMonitor.Services.OtaUpdateService.Parts;
using HSMonitor.Utils.Logger;
using NetSparkleUpdater;
using NetSparkleUpdater.Enums;
using NetSparkleUpdater.SignatureVerifiers;

namespace HSMonitor.Services.OtaUpdateService;

[SuppressMessage("Interoperability", "CA1416:Проверка совместимости платформы")]
public class OtaUpdateService
{
  private const int Chunk = 1024;
  
  private readonly SparkleUpdater _updater;
  private readonly ILogger<OtaUpdateService> _logger;
  
  private readonly Subject<DownloadPercentageEvent> _downloadProgressSubject = new();
  private readonly Subject<DownloadFinishedEvent> _downloadFinishedSubject = new();
  
  private UpdateInfo? _updateInfo;
  
  public OtaUpdateService(ILogger<OtaUpdateService> logger)
  {
    _logger = logger;
    _updater = new SparkleUpdater(App.DeviceAutoUpdateConfigUrl,
      new Ed25519Checker(SecurityMode.Unsafe))
    {
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
      LogWriter = null,
      CheckServerFileName = false,
      UpdateDownloader = null,
      AppCastDataDownloader = null,
    };
  }

  public IObservable<DownloadPercentageEvent> DownloadProgress => _downloadProgressSubject;
  public IObservable<DownloadFinishedEvent> DownloadFinished => _downloadFinishedSubject;

  public void SendOtaUpdate(byte[] data, string portName)
  {
    var crc = Crc32.Compute(data);
    
    using var sp = new SerialPort(portName, 921600, Parity.None, 8, StopBits.One);
    sp.ReadTimeout = 10000;
    sp.WriteTimeout = 10000;
    sp.DtrEnable = true;
    sp.RtsEnable = true;

    sp.Open();
        
    sp.DiscardInBuffer();
    sp.DiscardOutBuffer();
    Thread.Sleep(200);

    var client = new UsbOtaClient(sp);

    client.Hello();

    client.Begin((uint)data.Length, crc);

    int offset = 0;
    uint seq = 10;
    var started = DateTime.UtcNow;

    while (offset < data.Length)
    {
      var n = Math.Min(Chunk, data.Length - offset);
      client.Data(seq++, (uint)offset, data.AsSpan(offset, n));
      offset += n;

      if (seq % 50 == 0)
      {
        var progressRecord = new DownloadPercentageEvent(offset, data.Length);
        _logger.Info($"{progressRecord.ProgressPercentage}% ({offset}/{data.Length})");
        _downloadProgressSubject.OnNext(progressRecord);
      }
    }

    client.End(seq);
    
    _downloadFinishedSubject.OnNext(new DownloadFinishedEvent());

    var elapsed = DateTime.UtcNow - started;
    _logger.Info($"Done. Sent {data.Length} bytes in {elapsed.TotalSeconds:0.0}s");
    _logger.Info("Device should reboot into the new firmware.");
  }
}
