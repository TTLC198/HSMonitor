using System.Security.Principal;

namespace HSMonitor.Utils;

public class CheckRole
{
    public static bool IsUserAdministrator()
    {
        try
        {
            WindowsIdentity user = WindowsIdentity.GetCurrent();
            WindowsPrincipal principal = new WindowsPrincipal(user);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
        catch
        {
            return false;
        }
    }
}