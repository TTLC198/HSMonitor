using System;
using System.IO;
using System.Text.Json;

namespace HSMonitor.Utils;

public static class JsonExtensions
{
    public static T? JsonToItem<T>(this string jsonString) => JsonSerializer.Deserialize<T>(jsonString);
    public static (bool result, Exception exception) JsonToFile<T>(this T sender, string fileName, bool format = true)
    {
        try
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            File.WriteAllText(fileName, JsonSerializer.Serialize(sender, format ? options : null));
    
            return (true, null!);
        }
        catch (Exception exception)
        {
            return (false, exception);
        }
    }
}