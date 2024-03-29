﻿using System;
using System.Windows;
using HSMonitor.Services;
using HSMonitor.Utils.Logger;
using HSMonitor.ViewModels;
using HSMonitor.ViewModels.Framework;
using HSMonitor.ViewModels.Settings;
using Stylet;
using StyletIoC;

namespace HSMonitor.Utils;
#pragma warning disable CA1416
public class Bootstrapper : Bootstrapper<MainWindowViewModel>
{
    private T GetInstance<T>() => (T) base.GetInstance(typeof(T));
    
    protected override void OnStart()
    {
        Stylet.Logging.LogManager.LoggerFactory = _ => new FileLogger<Bootstrapper>();
        Stylet.Logging.LogManager.Enabled = false;
        base.OnStart();
    }
    
    protected override void ConfigureIoC(IStyletIoCBuilder builder)
    {
        base.ConfigureIoC(builder);

        builder.Bind(typeof(ILogger<>)).To(typeof(FileLogger<>));
        
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

    protected override void Configure()
    {
        GetInstance<SettingsService>().Load();
        base.Configure();
    }

    protected override void Launch()
    {
        GetInstance<HardwareMonitorService>().HardwareInformationUpdate(this, EventArgs.Empty);
        _ = GetInstance<DialogManager>().GetViewForDialogScreen(GetInstance<SettingsViewModel>());
        
        base.Launch();
    }
    
    protected override void OnExit(ExitEventArgs e)
    {
        GetInstance<SerialMonitorService>().Dispose();
        base.OnExit(e);
    }
}