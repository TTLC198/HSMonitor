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
    private GpuInformation _gpu = null!;
    private MemoryInformation _memory = null!;

    private ImageSource _cpuImageSource =
        new BitmapImage(new Uri(@"pack://application:,,,/HSMonitor;component/Resources/Images/UnknownLogo.png", UriKind.Absolute));

    private ImageSource _gpuImageSource =
        new BitmapImage(new Uri(@"pack://application:,,,/HSMonitor;component/Resources/Images/UnknownLogo.png", UriKind.Absolute));

    private ImageSource _memoryImageSource =
        new BitmapImage(new Uri(@"pack://application:,,,/HSMonitor;component/Resources/Images/DefaultRam.png", UriKind.Absolute));

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
        get => !_gpu.GpuFans.Any() ? new GpuFan() : _gpu.GpuFans.ToArray()[0];
        set
        {
            if (!_gpu.GpuFans.Any()) return;
            _gpu.GpuFans.ToArray()[0] = value;
            OnPropertyChanged();
        }
    }

    public GpuFan GpuFan2
    {
        get => !_gpu.GpuFans.Any() ? new GpuFan() : _gpu.GpuFans.ToArray()[1];
        set
        {
            if (!_gpu.GpuFans.Any()) return;
            _gpu.GpuFans.ToArray()[1] = value;
            OnPropertyChanged();
        }
    }

    public DashboardViewModel(HardwareMonitorService hardwareMonitorService, SettingsService settingsService)
    {
        _hardwareMonitorService = hardwareMonitorService;
        _settingsService = settingsService;
        _hardwareMonitorService.HardwareInformationUpdated += (_, _) => HardwareInformationUpdated();
        settingsService.SettingsSaved += UpdateImageFromSettings;
        
        HardwareInformationUpdated();
    }

    private void HardwareInformationUpdated()
    {
        Cpu = HardwareMonitorService.Cpu;
        Gpu = HardwareMonitorService.Gpu;
        Memory = HardwareMonitorService.Memory;
        if (_settingsService.Settings.IsAutoDetectHardwareEnabled) 
            UpdateImage();
        else 
            UpdateImageFromSettings(_settingsService, EventArgs.Empty);
    }

    private void UpdateImage()
    {
        if (_cpu is {Type: not null} and not null)
            CpuImageSource = Cpu.Type switch
            {
                var type when type!.Contains("Amd") => new BitmapImage(
                    new Uri(@"pack://application:,,,/HSMonitor;component/Resources/Images/AmdLogo.png",
                        UriKind.Absolute)),
                var type when type!.Contains("Intel") => new BitmapImage(
                    new Uri(@"pack://application:,,,/HSMonitor;component/Resources/Images/IntelLogo.png",
                        UriKind.Absolute)),
                _ => new BitmapImage(
                    new Uri(@"pack://application:,,,/HSMonitor;component/Resources/Images/UnknownLogo.png", 
                        UriKind.Absolute))
            };
        if (_gpu is {Type: not null} and not null)
            GpuImageSource = Gpu.Type switch
            {
                var type when type!.Contains("Amd") => new BitmapImage(
                    new Uri(@"pack://application:,,,/HSMonitor;component/Resources/Images/RadeonLogo.png", 
                        UriKind.Absolute)),
                var type when type!.Contains("Intel") => new BitmapImage(
                    new Uri(@"pack://application:,,,/HSMonitor;component/Resources/Images/IntelLogo.png", 
                        UriKind.Absolute)),
                var type when type!.Contains("Nvidia") => new BitmapImage(
                    new Uri(@"pack://application:,,,/HSMonitor;component/Resources/Images/NvidiaLogo.png", 
                        UriKind.Absolute)),
                _ => new BitmapImage(
                    new Uri(@"pack://application:,,,/HSMonitor;component/Resources/Images/UnknownLogo.png", 
                        UriKind.Absolute)),
            };
        if (_memory is {Type: not null} and not null)
            MemoryImageSource = Memory.Type switch
        {
            var type when type!.Contains("Trident") => new BitmapImage(
                new Uri(@"pack://application:,,,/HSMonitor;component/Resources/Images/TridentRam.png", 
                    UriKind.Absolute)),
            var type when type!.Contains("Viper") => new BitmapImage(
                new Uri(@"pack://application:,,,/HSMonitor;component/Resources/Images/ViperRam.png",
                    UriKind.Absolute)),
            _ => new BitmapImage(
                new Uri(@"pack://application:,,,/HSMonitor;component/Resources/Images/DefaultRam.png", 
                    UriKind.Absolute)),
        };
    }
    
    private void UpdateImageFromSettings(object? sender, EventArgs eventArgs)
    {
        if (sender is not SettingsService settingsService) return;
        if (settingsService is {Settings.IsAutoDetectHardwareEnabled: true} or null) return;
        CpuImageSource = settingsService.Settings.CpuCustomType switch
        {
            var type when type!.Contains("Amd") => new BitmapImage(
                new Uri(@"pack://application:,,,/HSMonitor;component/Resources/Images/AmdLogo.png",
                    UriKind.Absolute)),
            var type when type!.Contains("Intel") => new BitmapImage(
                new Uri(@"pack://application:,,,/HSMonitor;component/Resources/Images/IntelLogo.png",
                    UriKind.Absolute)),
            _ => new BitmapImage(
                new Uri(@"pack://application:,,,/HSMonitor;component/Resources/Images/UnknownLogo.png", 
                    UriKind.Absolute))
        };
        GpuImageSource = settingsService.Settings.GpuCustomType switch
        {
            var type when type!.Contains("Amd") => new BitmapImage(
                new Uri(@"pack://application:,,,/HSMonitor;component/Resources/Images/RadeonLogo.png", 
                    UriKind.Absolute)),
            var type when type!.Contains("Intel") => new BitmapImage(
                new Uri(@"pack://application:,,,/HSMonitor;component/Resources/Images/IntelLogo.png", 
                    UriKind.Absolute)),
            var type when type!.Contains("Nvidia") => new BitmapImage(
                new Uri(@"pack://application:,,,/HSMonitor;component/Resources/Images/NvidiaLogo.png", 
                    UriKind.Absolute)),
            _ => new BitmapImage(
                new Uri(@"pack://application:,,,/HSMonitor;component/Resources/Images/UnknownLogo.png", 
                    UriKind.Absolute)),
        };
        MemoryImageSource = settingsService.Settings.MemoryCustomType switch
        {
            var type when type!.Contains("Trident") => new BitmapImage(
                new Uri(@"pack://application:,,,/HSMonitor;component/Resources/Images/TridentRam.png", 
                    UriKind.Absolute)),
            var type when type!.Contains("Viper") => new BitmapImage(
                new Uri(@"pack://application:,,,/HSMonitor;component/Resources/Images/ViperRam.png",
                    UriKind.Absolute)),
            _ => new BitmapImage(
                new Uri(@"pack://application:,,,/HSMonitor;component/Resources/Images/DefaultRam.png", 
                    UriKind.Absolute)),
        };
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}