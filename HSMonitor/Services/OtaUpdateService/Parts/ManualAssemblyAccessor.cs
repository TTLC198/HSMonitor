using NetSparkleUpdater.Interfaces;

namespace HSMonitor.Services.OtaUpdateService.Parts;

sealed class ManualAssemblyAccessor : IAssemblyAccessor
{
  private readonly string _version;
  public ManualAssemblyAccessor(string version) => _version = version;

  public string AssemblyVersion => _version;

  public string AssemblyCompany => "ttlc198";
  public string AssemblyCopyright => "by ttlc198";
  public string AssemblyDescription => "HSMonitor Device";
  public string AssemblyTitle => "HSMonitor Device";
  public string AssemblyProduct => "HSMonitor Device";
}