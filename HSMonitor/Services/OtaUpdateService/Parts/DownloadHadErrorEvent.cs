using NetSparkleUpdater;

namespace HSMonitor.Services.OtaUpdateService.Parts;

public record DownloadHadErrorEvent(AppCastItem Item, string? Path, Exception Exception);
