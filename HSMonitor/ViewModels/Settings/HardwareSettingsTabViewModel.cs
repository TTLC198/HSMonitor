using HSMonitor.Services;
using HSMonitor.Services.HardwareMonitorService;
using LibreHardwareMonitor.Hardware;
using LibreHardwareMonitor.Hardware.Cpu;

namespace HSMonitor.ViewModels.Settings;

public class HardwareSettingsTabViewModel(SettingsService settingsService) : SettingsTabBaseViewModel(settingsService, 1, "Hardware")
{
    private readonly SettingsService _settingsService = settingsService;

    public List<IHardware> Processors { get; private set; } = [];

    public List<IHardware> GraphicCards { get; private set; } = [];

    public IHardware SelectedCpu
    {
        get => field;
        set
        {
            _settingsService.Settings.CpuId =
                (Processors.FirstOrDefault(c => c.Identifier.ToString()!.Contains(value.Identifier.ToString() ?? string.Empty)) ?? Processors.First()).Identifier
                .ToString();
            field = value;
            OnPropertyChanged(nameof(SelectedCpu));
        }
    }

    public IHardware SelectedGpu
    {
        get => field;
        set
        {
            _settingsService.Settings.GpuId =
                (GraphicCards.FirstOrDefault(c => c.Identifier.ToString()!.Contains(value?.Identifier.ToString() ?? string.Empty)) ?? GraphicCards.First()).Identifier
                .ToString();
            field = value;
            OnPropertyChanged(nameof(SelectedGpu));
        }
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

    public async Task OnViewFullyLoaded()
    {
        Processors = HardwareMonitorServiceImpl.GetProcessors().ToList();
        GraphicCards = HardwareMonitorServiceImpl.GetGraphicCards().ToList();
        
        if (SettingsService is not {Settings: not null}) return;
        if (Processors is {Count: > 0})
        {
            SelectedCpu = Processors
                              .FirstOrDefault(c =>
                                  c.Identifier.ToString()!.Contains(_settingsService.Settings.CpuId ?? ""))
                       ?? Processors.First();
            OnPropertyChanged(nameof(SelectedCpu));
        }
        if (GraphicCards is {Count: > 0})
        {
            SelectedGpu = GraphicCards
                              .FirstOrDefault(c =>
                                  c.Identifier.ToString()!.Contains(_settingsService.Settings.GpuId ?? ""))
                       ?? GraphicCards.First();
            OnPropertyChanged(nameof(SelectedGpu));
        }
    }
}