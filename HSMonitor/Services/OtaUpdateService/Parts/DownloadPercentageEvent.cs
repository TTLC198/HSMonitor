namespace HSMonitor.Services.OtaUpdateService.Parts;

public record DownloadPercentageEvent
{
  public DownloadPercentageEvent(int bytesReceived, int totalBytesToReceive)
  {
    BytesReceived = bytesReceived;
    TotalBytesToReceive = totalBytesToReceive;
  }
  
  public int ProgressPercentage => 100 * (BytesReceived / TotalBytesToReceive);
  
  /// <summary>The number of bytes received by the downloader</summary>
  public int BytesReceived { get; private set; }

  /// <summary>The total number of bytes that need to be downloaded</summary>
  public int TotalBytesToReceive { get; private set; }
}
