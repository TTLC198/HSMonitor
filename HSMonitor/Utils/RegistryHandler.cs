using Microsoft.Win32;

namespace HSMonitor.Utils;
#pragma warning disable CA1416
public class RegistryHandler
{
    private readonly string _registryValue;
    private readonly string _subKey;
    public string[] Args { get; set; }

    public bool IsSet
    {
        get
        {
            using var reg = Registry.CurrentUser.OpenSubKey(_subKey) ?? throw new InvalidOperationException("Registry key is null");
            var equal = EqualityComparer<object>.Default.Equals(string.Join(" ", _registryValue, string.Join(" ", Args)) , reg.GetValue(App.Name));
            return equal;
        }
        set
        {
            using var reg = Registry.CurrentUser.CreateSubKey(_subKey) ?? throw new InvalidOperationException("Registry key is null");
            if (IsSet == value)
                return;
            if (value)
                reg.SetValue(App.Name, string.Join(" ", _registryValue, string.Join(" ", Args)));
            else
                reg.DeleteValue(App.Name);
        }
    }

    public RegistryHandler(string subKey, string registryValue, string[] args)
    {
        _subKey = subKey;
        _registryValue = registryValue;
        Args = args;
    }
}