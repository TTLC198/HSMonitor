using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using HSMonitor.Services;
using HSMonitor.ViewModels.Settings;

namespace HSMonitor
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App
    {
        private static Assembly Assembly { get; } = typeof(App).Assembly;

        public static string Name { get; } = Assembly.GetName().Name!;

        public static Version Version { get; } = Assembly.GetName().Version!;

        public static string VersionString { get; } = "v" + Version.ToString(3).Trim();

        public static string SettingsDirPath { get;  } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), Name);
        
        public static string LogsDirPath { get;  } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), Name, "logs");
        
        public static string ExecutableDirPath { get; } = AppDomain.CurrentDomain.BaseDirectory!;

        [RequiresAssemblyFiles()]
        public static string ExecutableFilePath { get; } = Path.ChangeExtension(typeof(App).Assembly.Location, "exe");

        public static string GitHubProjectUrl { get; } = 
            "https://github.com/TTLC198/HSMonitor";
        
        public static string GitHubClientProjectUrl { get; } = 
            "https://github.com/TTLC198/HSMonitorClient";

        public static string GitHubAutoUpdateConfigUrl { get; } =
            "https://raw.githubusercontent.com/TTLC198/HSMonitor/master/appcast.xml";
    }

    public partial class App
    {
        private static IReadOnlyList<string> CommandLineArgs { get; } = Environment.GetCommandLineArgs();

        public static string HiddenOnLaunchArgument { get; } = "--minimize";

        public static bool IsHiddenOnLaunch { get; } = CommandLineArgs.Contains(
            HiddenOnLaunchArgument,
            StringComparer.OrdinalIgnoreCase
        );
    }
}