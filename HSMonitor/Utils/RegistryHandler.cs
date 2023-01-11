using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace HSMonitor.Utils;

public class RegistryHandler
{
    private readonly object _registryValue;
    private readonly string _subKey;

    public bool IsSet
    {
        get
        {
            using var reg = Registry.CurrentUser.OpenSubKey(_subKey);
            var temp = reg.GetValue(App.Name);
            var equal = EqualityComparer<object>.Default.Equals(_registryValue, reg.GetValue(App.Name));
            return equal;
        }
        set
        {
            using var reg = Registry.CurrentUser.CreateSubKey(_subKey);
            if (reg is null) throw new Exception("Registry key is null");
            if (IsSet == value)
                return;
            if (value)
                reg.SetValue(App.Name, _registryValue);
            else
                reg.DeleteValue(App.Name);
        }
    }

    public RegistryHandler(string subKey, string registryValue)
    {
        _subKey = subKey;
        _registryValue = registryValue;
    }
}