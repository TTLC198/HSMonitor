using System;
using System.IO;
using System.Text.Json;

namespace HSMonitor.Utils;

public static class JsonExtensions
{
    public static void JsonToFile<T>(this T sender, string fileName, bool format = true)
    {
        var options = new JsonSerializerOptions { WriteIndented = true };
        File.WriteAllText(fileName, JsonSerializer.Serialize(sender, format ? options : null));
    }
}