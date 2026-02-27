using System.Diagnostics;
using System.IO;
using HSMonitor.Views;

namespace HSMonitor.Utils;

public static class InstallerService
{
    static void InstallPawnIo()
    {
        var path = ExtractPawnIo();
        if (string.IsNullOrEmpty(path)) return;
        var process = Process.Start(new ProcessStartInfo(path, "-install"));
        process?.WaitForExit();

        File.Delete(path);
    }
    
    static string ExtractPawnIo()
    {
        var destination = Path.Combine(Directory.GetCurrentDirectory(), "PawnIO_setup.exe");

        try
        {
            using var resourceStream = typeof(MainWindowView).Assembly.GetManifestResourceStream("HSMonitor.Resources.PawnIO_setup.exe");
            using FileStream fileStream = new(destination, FileMode.Create, FileAccess.Write);
            resourceStream?.CopyTo(fileStream);

            return destination;
        }
        catch
        {
            return null;
        }
    }
}