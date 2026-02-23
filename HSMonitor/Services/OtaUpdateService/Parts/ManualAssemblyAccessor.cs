using NetSparkleUpdater.Interfaces;

namespace HSMonitor.Services.OtaUpdateService.Parts;

sealed class ManualAssemblyAccessor(Func<string> version) : IAssemblyAccessor
{
  private readonly string _version = version();

  public string AssemblyVersion => _version;
  public string AssemblyCompany => "ttlc198";
  public string AssemblyCopyright => "by ttlc198";
  public string AssemblyDescription => "HSMonitor Device";
  public string AssemblyTitle => "HSMonitor Device";
  public string AssemblyProduct => "HSMonitor Device";
}