using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using HSMonitor.Models;
using HSMonitor.Services;

namespace HSMonitor.ViewModels;

public class DashboardViewModel : INotifyPropertyChanged
{
    private readonly HardwareMonitorService _hardwareMonitorService;
    private readonly SettingsService _settingsService;

    private CpuInformation _cpu = null!;

    private ImageSource _cpuImageSource =
        new BitmapImage(new Uri(@"pack://application:,,,/HSMonitor;component/Resources/Images/UnknownLogo.png",
            UriKind.Absolute));

    private double _displayOpacity = 1;
    private GpuInformation _gpu = null!;

    private ImageSource _gpuImageSource =
        new BitmapImage(new Uri(@"pack://application:,,,/HSMonitor;component/Resources/Images/UnknownLogo.png",
            UriKind.Absolute));

    private MemoryInformation _memory = null!;

    private ImageSource _memoryImageSource =
        new BitmapImage(new Uri(@"pack://application:,,,/HSMonitor;component/Resources/Images/DefaultRam.png",
            UriKind.Absolute));

    public DashboardViewModel(HardwareMonitorService hardwareMonitorService, SettingsService settingsService)
    {
        _hardwareMonitorService = hardwareMonitorService;
        _settingsService = settingsService;
        hardwareMonitorService.HardwareInformationUpdated += (_, _) => HardwareInformationUpdated();
        settingsService.SettingsSaved += UpdateImageFromSettings;

        HardwareInformationUpdated();
    }

    public ImageSource CpuImageSource
    {
        get => _cpuImageSource;
        private set
        {
            _cpuImageSource = value;
            OnPropertyChanged();
        }
    }

    public ImageSource GpuImageSource
    {
        get => _gpuImageSource;
        private set
        {
            _gpuImageSource = value;
            OnPropertyChanged();
        }
    }

    public ImageSource MemoryImageSource
    {
        get => _memoryImageSource;
        private set
        {
            _memoryImageSource = value;
            OnPropertyChanged();
        }
    }

    public CpuInformation Cpu
    {
        get => _cpu;
        private set
        {
            _cpu = value;
            OnPropertyChanged();
        }
    }

    public GpuInformation Gpu
    {
        get => _gpu;
        private set
        {
            _gpu = value;
            OnPropertyChanged();
        }
    }

    public MemoryInformation Memory
    {
        get => _memory;
        private set
        {
            _memory = value;
            OnPropertyChanged();
        }
    }

    public GpuFan GpuFan1
    {
        get => _gpu is {GpuFans: null} ? new GpuFan() : _gpu.GpuFans.ToArray().ElementAtOrDefault(0) ?? new GpuFan();
        set
        {
            if (_gpu is {GpuFans: null} || _gpu.GpuFans.ToArray().ElementAtOrDefault(0) is null) return;
            _gpu.GpuFans.ToArray()[0] = value;
            OnPropertyChanged();
        }
    }

    public GpuFan GpuFan2
    {
        get => _gpu is {GpuFans: null} ? new GpuFan() : _gpu.GpuFans.ToArray().ElementAtOrDefault(1) ?? new GpuFan();
        set
        {
            if (_gpu is {GpuFans: null} || _gpu.GpuFans.ToArray().ElementAtOrDefault(1) is null) return;
            _gpu.GpuFans.ToArray()[1] = value;
            OnPropertyChanged();
        }
    }

    public double DisplayOpacity
    {
        get => _displayOpacity;
        set
        {
            _displayOpacity = value;
            OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void HardwareInformationUpdated()
    {
        Cpu = _hardwareMonitorService.Cpu;
        Gpu = _hardwareMonitorService.Gpu;
        Memory = _hardwareMonitorService.Memory;

        if (Cpu.Name is {Length: > 23})
            Cpu.Name = Cpu.Name[..23];
        if (Gpu.Name is {Length: > 23})
            Gpu.Name = Gpu.Name[..23];

        if (_settingsService is {Settings: null}) return;
        if (_settingsService.Settings.IsAutoDetectHardwareEnabled)
            UpdateImageFromComputer();
        else
            UpdateImageFromSettings(_settingsService, EventArgs.Empty);
    }

    private void UpdateImageFromComputer()
    {
        if (_cpu is {Type: not null} and not null)
            UpdateCpuImage(_cpu.Type);
        if (_gpu is {Type: not null} and not null)
            UpdateGpuImage(_gpu.Type);
        if (_memory is {Type: not null} and not null)
            UpdateRamImage(_memory.Type);
    }

    private void UpdateImageFromSettings(object? sender, EventArgs eventArgs)
    {
        if (sender is not SettingsService settingsService) return;
        DisplayOpacity = (double) settingsService.Settings.DeviceDisplayBrightness / 100;
        if (settingsService is {Settings.IsAutoDetectHardwareEnabled: true} or null) return;
        if (settingsService.Settings.CpuCustomType is not null)
            UpdateCpuImage(settingsService.Settings.CpuCustomType);
        if (settingsService.Settings.GpuCustomType is not null)
            UpdateGpuImage(settingsService.Settings.GpuCustomType);
        if (settingsService.Settings.MemoryCustomType is not null)
            UpdateRamImage(settingsService.Settings.MemoryCustomType);
    }

    private void UpdateCpuImage(string cpuType)
    {
        CpuImageSource = cpuType switch
        {
            _ when cpuType!.Contains("Amd") => new BitmapImage(
                new Uri(@"pack://application:,,,/HSMonitor;component/Resources/Images/AmdLogo.png",
                    UriKind.Absolute)),
            _ when cpuType!.Contains("Intel") => new BitmapImage(
                new Uri(@"pack://application:,,,/HSMonitor;component/Resources/Images/IntelLogo.png",
                    UriKind.Absolute)),
            _ => new BitmapImage(
                new Uri(@"pack://application:,,,/HSMonitor;component/Resources/Images/UnknownLogo.png",
                    UriKind.Absolute))
        };
    }

    private void UpdateGpuImage(string gpuType)
    {
        GpuImageSource = gpuType switch
        {
            _ when gpuType!.Contains("Amd") => new BitmapImage(
                new Uri(@"pack://application:,,,/HSMonitor;component/Resources/Images/RadeonLogo.png",
                    UriKind.Absolute)),
            _ when gpuType!.Contains("Intel") => new BitmapImage(
                new Uri(@"pack://application:,,,/HSMonitor;component/Resources/Images/IntelLogo.png",
                    UriKind.Absolute)),
            _ when gpuType!.Contains("Nvidia") => new BitmapImage(
                new Uri(@"pack://application:,,,/HSMonitor;component/Resources/Images/NvidiaLogo.png",
                    UriKind.Absolute)),
            _ => new BitmapImage(
                new Uri(@"pack://application:,,,/HSMonitor;component/Resources/Images/UnknownLogo.png",
                    UriKind.Absolute))
        };
    }

    private void UpdateRamImage(string ramType)
    {
        MemoryImageSource = ramType switch
        {
            _ when ramType!.Contains("Trident") => new BitmapImage(
                new Uri(@"pack://application:,,,/HSMonitor;component/Resources/Images/TridentRam.png",
                    UriKind.Absolute)),
            _ when ramType!.Contains("Viper") => new BitmapImage(
                new Uri(@"pack://application:,,,/HSMonitor;component/Resources/Images/ViperRam.png",
                    UriKind.Absolute)),
            _ => new BitmapImage(
                new Uri(@"pack://application:,,,/HSMonitor;component/Resources/Images/DefaultRam.png",
                    UriKind.Absolute))
        };
    }

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}