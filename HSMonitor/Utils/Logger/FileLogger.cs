using System;
using System.IO;
using System.Linq;
using Stylet.Logging;

namespace HSMonitor.Utils.Logger;

public interface ILogger<T> : ILogger {}

public class FileLogger<T> : ILogger<T>
{
    private readonly string _fullFilePath;
    private static object _lock = new object();

    public FileLogger()
    {
        _fullFilePath = Path.Combine(App.LogsDirPath, DateTime.Now.ToString("yyyy-MM-dd") + "_log.txt");
        DeleteOldLogFiles();
    }

    public void Info(string format, params object[] args)
    {
        Log(nameof(Info).ToUpper(), message: format);
    }

    public void Warn(string format, params object[] args)
    {
        Log(nameof(Warn).ToUpper(), message: format);
    }

    public void Error(Exception exception, string message = null)
    {
        Log(nameof(Error).ToUpper(), exception, message);
    }

    private void Log(string logLevel, Exception? exception = null, string? message = "")
    {
        lock (_lock)
        {
            var n = Environment.NewLine;
            if (exception != null) 
                message += $"{n} {exception.GetType()} - {exception.Message} - {exception.StackTrace}";
            Directory.CreateDirectory(App.LogsDirPath);
            File.AppendAllText(_fullFilePath, $"{DateTime.Now} | {logLevel} | {typeof(T)} - {message} {n}");
        }
    }

    private void DeleteOldLogFiles()
    {
        Directory
            .GetFiles(App.LogsDirPath)
            .Select(f => new FileInfo(f))
            .Where(f => f.CreationTime < DateTime.Now.AddMonths(-3))
            .ToList()
            .ForEach(f => f.Delete());
    }
}