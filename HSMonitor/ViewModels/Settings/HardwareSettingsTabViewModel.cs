using System.Collections.Generic;
using System.Linq;
using HSMonitor.Services;
using LibreHardwareMonitor.Hardware;

namespace HSMonitor.ViewModels.Settings;

public class HardwareSettingsTabViewModel : SettingsTabBaseViewModel
{
    private readonly SettingsService _settingsService;

    public HardwareSettingsTabViewModel(SettingsService settingsService)
        : base(settingsService, 1, "Hardware")
    {
        _settingsService = settingsService;
        if (SettingsService is not {Settings: not null}) return;
        if (Processors is {Count: > 0})
            SelectedCpu = Processors
                              .FirstOrDefault(c =>
                                  c.Identifier.ToString()!.Contains(_settingsService.Settings.CpuId ?? ""))
                          ?? Processors.First();
        if (GraphicCards is {Count: > 0})
            SelectedGpu = GraphicCards
                              .FirstOrDefault(c =>
                                  c.Identifier.ToString()!.Contains(_settingsService.Settings.GpuId ?? ""))
                          ?? GraphicCards.First();
    }

    public List<IHardware> Processors => HardwareMonitorService.GetProcessors().ToList();

    public List<IHardware> GraphicCards => HardwareMonitorService.GetGraphicCards().ToList();

    public IHardware SelectedCpu
    {
        get =>
            Processors
                .FirstOrDefault(c =>
                    c.Identifier.ToString()!.Contains(_settingsService.Settings.CpuId ?? ""))
            ?? Processors.First();
        set =>
            _settingsService.Settings.CpuId =
                (Processors
                     .FirstOrDefault(c =>
                         c.Identifier.ToString()!.Contains(value.Identifier.ToString() ?? string.Empty))
                 ?? Processors.First()).Identifier
                .ToString();
    }

    public IHardware SelectedGpu
    {
        get => GraphicCards
                   .FirstOrDefault(c =>
                       c.Identifier.ToString()!.Contains(_settingsService.Settings.GpuId ?? ""))
               ?? GraphicCards.First();
        set => _settingsService.Settings.GpuId =
            (GraphicCards
                 .FirstOrDefault(c =>
                     c.Identifier.ToString()!.Contains(value.Identifier.ToString() ?? string.Empty))
             ?? GraphicCards.First()).Identifier
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
}