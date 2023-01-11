using System.Threading;
using HSMonitor.Services;
using HSMonitor.ViewModels;
using HSMonitor.ViewModels.Framework;
using HSMonitor.ViewModels.Settings;
using Stylet;
using StyletIoC;

namespace HSMonitor.Utils;

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
        // Load settings (this has to come before any view is loaded because bindings are not updated)
        GetInstance<SettingsService>().Load();
        
        GetInstance<HardwareMonitorService>().HardwareInformationUpdate();

        // Stylet/WPF is slow, so we preload all dialogs, including descendants, for smoother UX
        _ = GetInstance<DialogManager>().GetViewForDialogScreen(GetInstance<SettingsViewModel>());

        base.Launch();
    }
}