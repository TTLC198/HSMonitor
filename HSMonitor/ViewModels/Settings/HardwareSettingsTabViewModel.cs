using System;
using System.Collections.Generic;
using System.Linq;
using HSMonitor.Models;
using HSMonitor.Services;
using LibreHardwareMonitor.Hardware;

namespace HSMonitor.ViewModels.Settings;

public class HardwareSettingsTabViewModel : SettingsTabBaseViewModel
{
    private readonly SettingsService _settingsService;

    public IEnumerable<IHardware> Processors => HardwareMonitorService.GetProcessors();

    public IEnumerable<IHardware> GraphicCards => HardwareMonitorService.GetGraphicCards();

    public IHardware SelectedCpu
    {
        get => HardwareMonitorService
                   .GetProcessors()
                   .FirstOrDefault(c =>
                       c.Identifier
                           .ToString()
                           .Contains(_settingsService.Settings.CpuId ?? ""))
               ?? HardwareMonitorService.GetProcessors().First();
        set => _settingsService.Settings.CpuId =
            (HardwareMonitorService
                 .GetProcessors()
                 .FirstOrDefault(c =>
                     c.Identifier
                         .ToString()
                         .Contains(value.Identifier.ToString()))
             ?? HardwareMonitorService.GetProcessors().First()).Identifier
            .ToString();
    }

    public IHardware SelectedGpu
    {
        get => HardwareMonitorService
                   .GetGraphicCards()
                   .FirstOrDefault(c =>
                       c.Identifier
                           .ToString()
                           .Contains(_settingsService.Settings.GpuId ?? ""))
               ?? HardwareMonitorService.GetGraphicCards().First();
        set => _settingsService.Settings.GpuId =
            (HardwareMonitorService
                 .GetGraphicCards()
                 .FirstOrDefault(c =>
                     c.Identifier
                         .ToString()
                         .Contains(value.Identifier.ToString()))
             ?? HardwareMonitorService.GetGraphicCards().First()).Identifier
            .ToString();
    }

    public int DefaultCpuFrequency
    {
        get => SettingsService.Settings.DefaultCpuFrequency;
        set => SettingsService.Settings.DefaultCpuFrequency = value;
    }

    public int DefaultGpuFrequency
    {
        get => SettingsService.Settings.DefaultGpuFrequency;
        set => SettingsService.Settings.DefaultGpuFrequency = value;
    }

    public HardwareSettingsTabViewModel(SettingsService settingsService)
        : base(settingsService, 1, "Hardware")
    {
        _settingsService = settingsService;
        if (SettingsService is not {Settings: not null}) return;
        SelectedCpu = HardwareMonitorService
                          .GetProcessors()
                          .FirstOrDefault(c =>
                              c.Identifier
                                  .ToString()
                                  .Contains(_settingsService.Settings.CpuId ?? ""))
                      ?? HardwareMonitorService.GetProcessors().First();
        SelectedGpu = HardwareMonitorService
                          .GetGraphicCards()
                          .FirstOrDefault(c =>
                              c.Identifier
                                  .ToString()
                                  .Contains(_settingsService.Settings.GpuId ?? ""))
                      ?? HardwareMonitorService.GetGraphicCards().First();
    }
}