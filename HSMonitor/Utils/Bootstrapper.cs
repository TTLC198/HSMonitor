using System.Threading;
using System.Windows;
using HSMonitor.Services;
using HSMonitor.ViewModels;
using HSMonitor.ViewModels.Framework;
using HSMonitor.ViewModels.Settings;
using Stylet;
using StyletIoC;
using MessageBoxViewModel = HSMonitor.ViewModels.MessageBoxViewModel;

namespace HSMonitor.Utils;
#pragma warning disable CA1416
public class Bootstrapper : Bootstrapper<MainWindowViewModel>
{
    private T GetInstance<T>() => (T) base.GetInstance(typeof(T));

    protected override void ConfigureIoC(IStyletIoCBuilder builder)
    {
        base.ConfigureIoC(builder);

        builder.Bind<HardwareMonitorService>().ToSelf().InSingletonScope();
        builder.Bind<SettingsService>().ToSelf().InSingletonScope();
        builder.Bind<SerialMonitorService>().ToSelf().InSingletonScope();
        builder.Bind<DialogManager>().ToSelf().InSingletonScope();
        
        builder.Bind<IViewModelFactory>().ToAbstractFactory();

        builder.Bind<MainWindowViewModel>().ToSelf().InSingletonScope();
        builder.Bind<DashboardViewModel>().ToSelf().InSingletonScope();
        builder.Bind<SettingsViewModel>().ToSelf().InSingletonScope();
        builder.Bind<ISettingsTabViewModel>().ToAllImplementations().InSingletonScope();
    }
    
    protected override void Launch()
    {
        GetInstance<HardwareMonitorService>().HardwareInformationUpdate();
        _ = GetInstance<DialogManager>().GetViewForDialogScreen(GetInstance<SettingsViewModel>());

        base.Launch();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        GetInstance<SerialMonitorService>().Dispose();
        base.OnExit(e);
    }
}
#pragma warning restore CA1416