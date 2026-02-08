namespace HSMonitor.Services.OtaUpdateService.Parts;

public record DownloadPercentageEvent
{
  public DownloadPercentageEvent(long bytesReceived, long totalBytesToReceive)
  {
    BytesReceived = bytesReceived;
    TotalBytesToReceive = totalBytesToReceive;
  }
  
  public int ProgressPercentage => (int)(100 * ((double)BytesReceived / (double)TotalBytesToReceive));
  
  /// <summary>The number of bytes received by the downloader</summary>
  public long BytesReceived { get; private set; }

  /// <summary>The total number of bytes that need to be downloaded</summary>
  public long TotalBytesToReceive { get; private set; }
}
