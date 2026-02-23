using System.Globalization;
using System.Resources;
using HSMonitor.Properties;

namespace HSMonitor.Utils;

public static class LocalizationManager
{
    public static IEnumerable<CultureInfo> GetAvailableCultures()
    {
        var result = new List<CultureInfo>();

        var rm = new ResourceManager(typeof(Resources));

        var cultures = CultureInfo.GetCultures(CultureTypes.AllCultures);
        foreach (var culture in cultures)
        {
            try
            {
                if (culture.Equals(CultureInfo.InvariantCulture)) continue; //do not use "==", won't work

                var rs = rm.GetResourceSet(culture, true, false);
                if (rs != null)
                    result.Add(culture);
            }
            catch (CultureNotFoundException)
            {
                //NOP
            }
        }
        return result;
    }

    public static void ChangeCurrentCulture(CultureInfo cultureInfo)
    {
        Thread.CurrentThread.CurrentCulture = cultureInfo;
        Thread.CurrentThread.CurrentUICulture = cultureInfo;
    }

    public static CultureInfo GetCurrentCulture()
    {
        return Thread.CurrentThread.CurrentCulture;
    }
}