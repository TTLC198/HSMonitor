using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using HSMonitor.Models;
using HSMonitor.Services;
using HSMonitor.Services.HardwareMonitorService;

namespace HSMonitor.ViewModels;

public class DashboardViewModel : INotifyPropertyChanged
{
    private readonly SettingsService _settingsService;
    private readonly HardwareMonitorServiceImpl _hardwareMonitorServiceImpl;

    private CpuInformation _cpu = null!;
    private GpuInformation _gpu = null!;
    private MemoryInformation _memory = null!;

    private const int GraphSamples = 60;
    private readonly double[] _gpuPowerSamples = new double[GraphSamples];
    private readonly double[] _gpuLoadSamples = new double[GraphSamples];
    private int _gpuGraphHead;

    public ImageSource CpuImageSource
    {
        get;
        private set
        {
            field = value;
            OnPropertyChanged();
        }
    } = new BitmapImage(new Uri(@"pack://application:,,,/HSMonitor;component/Resources/Images/UnknownLogo.png", UriKind.Absolute));

    public ImageSource GpuImageSource
    {
        get;
        private set
        {
            field = value;
            OnPropertyChanged();
        }
    } = new BitmapImage(new Uri(@"pack://application:,,,/HSMonitor;component/Resources/Images/UnknownLogo.png", UriKind.Absolute));

    public ImageSource MemoryImageSource
    {
        get;
        private set
        {
            field = value;
            OnPropertyChanged();
        }
    } = new BitmapImage(new Uri(@"pack://application:,,,/HSMonitor;component/Resources/Images/DefaultRam.png", UriKind.Absolute));

    public CpuInformation Cpu
    {
        get => _cpu;
        private set { _cpu = value; OnPropertyChanged(); }
    }

    public GpuInformation Gpu
    {
        get => _gpu;
        private set { _gpu = value; OnPropertyChanged(); }
    }

    public MemoryInformation Memory
    {
        get => _memory;
        private set { _memory = value; OnPropertyChanged(); }
    }

    public double DisplayOpacity
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged();
        }
    } = 1;

    public string CpuOcMarqueeText
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged();
        }
    } = "";

    public string GpuOcMarqueeText
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged();
        }
    } = "";

    public bool IsCpuBoostActive
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged();
        }
    }

    public bool IsGpuBoostActive
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged();
        }
    }

    public double CpuBoostDeltaMHz
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof (CpuBoostMarqueeText));
        }
    }

    public double GpuBoostDeltaMHz
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof (GpuBoostMarqueeText));
        }
    }

    public string CpuBoostMarqueeText => $"BOOST +{CpuBoostDeltaMHz:0}MHz";
    public string GpuBoostMarqueeText => $"BOOST +{GpuBoostDeltaMHz:0}MHz";

    public PointCollection GpuPowerGraphPoints
    {
        get;
        private set
        {
            field = value;
            OnPropertyChanged();
        }
    } = new();

    public PointCollection GpuLoadGraphPoints
    {
        get;
        private set
        {
            field = value;
            OnPropertyChanged();
        }
    } = new();

    public DashboardViewModel(HardwareMonitorServiceImpl hardwareMonitorServiceImpl, SettingsService settingsService)
    {
        _hardwareMonitorServiceImpl = hardwareMonitorServiceImpl;
        _settingsService = settingsService;

        hardwareMonitorServiceImpl.HardwareInformationUpdated += (_, _) => HardwareInformationUpdated();
        settingsService.SettingsSaved += UpdateImageFromSettings;

        HardwareInformationUpdated();
    }

    private void HardwareInformationUpdated()
    {
        Cpu = _hardwareMonitorServiceImpl.Cpu;
        Gpu = _hardwareMonitorServiceImpl.Gpu;
        Memory = _hardwareMonitorServiceImpl.Memory;
        
        if (string.IsNullOrWhiteSpace(CpuOcMarqueeText)) 
            CpuOcMarqueeText = "+0MHz  +0MHz  +0MHz  +0MHz  +0MHz  +0MHz  +0MHz  +0MHz";
        if (string.IsNullOrWhiteSpace(GpuOcMarqueeText)) 
            GpuOcMarqueeText = "+0MHz  +0MHz  +0MHz  +0MHz  +0MHz  +0MHz  +0MHz  +0MHz";

        CpuBoostDeltaMHz = Cpu.Clock - _settingsService.Settings.DefaultCpuFrequency;
        GpuBoostDeltaMHz = Gpu.CoreClock - _settingsService.Settings.DefaultGpuFrequency;

        IsCpuBoostActive = CpuBoostDeltaMHz > 0;
        IsGpuBoostActive = GpuBoostDeltaMHz > 0;

        UpdateGpuGraphs();

        if (_settingsService is { Settings: null }) return;
        if (_settingsService.Settings.IsAutoDetectHardwareEnabled)
            UpdateImageFromComputer();
        else
            UpdateImageFromSettings(_settingsService, EventArgs.Empty);
    }

    private void UpdateImageFromComputer()
    {
        if (_cpu is { Type: not null }) 
            UpdateCpuImage(_cpu.Type);
        if (_gpu is { Type: not null }) 
            UpdateGpuImage(_gpu.Type);
        if (_memory is { Type: not null }) 
            UpdateRamImage(_memory.Type);
    }

    private void UpdateImageFromSettings(object? sender, EventArgs eventArgs)
    {
        if (sender is not SettingsService settingsService) return;
        DisplayOpacity = (double)settingsService.Settings.DeviceDisplayBrightness / 100;

        if (settingsService is { Settings.IsAutoDetectHardwareEnabled: true } or null) return;

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
            _ when cpuType.Contains("Amd") => new BitmapImage(
                new Uri(@"pack://application:,,,/HSMonitor;component/Resources/Images/AmdLogo.png", UriKind.Absolute)),
            _ when cpuType.Contains("Intel") => new BitmapImage(
                new Uri(@"pack://application:,,,/HSMonitor;component/Resources/Images/IntelLogo.png", UriKind.Absolute)),
            _ => new BitmapImage(
                new Uri(@"pack://application:,,,/HSMonitor;component/Resources/Images/UnknownLogo.png", UriKind.Absolute))
        };
    }

    private void UpdateGpuImage(string gpuType)
    {
        GpuImageSource = gpuType switch
        {
            _ when gpuType.Contains("Amd") => new BitmapImage(
                new Uri(@"pack://application:,,,/HSMonitor;component/Resources/Images/RadeonLogo.png", UriKind.Absolute)),
            _ when gpuType.Contains("Intel") => new BitmapImage(
                new Uri(@"pack://application:,,,/HSMonitor;component/Resources/Images/IntelLogo.png", UriKind.Absolute)),
            _ when gpuType.Contains("Nvidia") => new BitmapImage(
                new Uri(@"pack://application:,,,/HSMonitor;component/Resources/Images/NvidiaLogo.png", UriKind.Absolute)),
            _ => new BitmapImage(
                new Uri(@"pack://application:,,,/HSMonitor;component/Resources/Images/UnknownLogo.png", UriKind.Absolute)),
        };
    }

    private void UpdateRamImage(string ramType)
    {
        MemoryImageSource = ramType switch
        {
            _ when ramType.Contains("Trident") => new BitmapImage(
                new Uri(@"pack://application:,,,/HSMonitor;component/Resources/Images/TridentRam.png", UriKind.Absolute)),
            _ when ramType.Contains("Viper") => new BitmapImage(
                new Uri(@"pack://application:,,,/HSMonitor;component/Resources/Images/ViperRam.png", UriKind.Absolute)),
            _ => new BitmapImage(
                new Uri(@"pack://application:,,,/HSMonitor;component/Resources/Images/DefaultRam.png", UriKind.Absolute)),
        };
    }

    private void UpdateGpuGraphs()
    {
        const double w = 110;
        const double h = 32;

        var power = _gpu?.Power ?? 0;
        var load = _gpu?.CoreLoad ?? 0;

        _gpuPowerSamples[_gpuGraphHead] = Math.Max(0, power);
        _gpuLoadSamples[_gpuGraphHead] = Math.Clamp(load, 0, 100);
        _gpuGraphHead = (_gpuGraphHead + 1) % GraphSamples;

        var maxPower = 1.0;
        for (var i = 0; i < GraphSamples; i++)
            maxPower = Math.Max(maxPower, _gpuPowerSamples[i]);
        maxPower = Math.Max(maxPower, 50);

        var pPoints = new PointCollection(GraphSamples);
        var lPoints = new PointCollection(GraphSamples);

        for (var i = 0; i < GraphSamples; i++)
        {
            var idx = (_gpuGraphHead + i) % GraphSamples; // oldest -> newest
            var x = i * (w / (GraphSamples - 1.0));

            var py = h - (_gpuPowerSamples[idx] / maxPower) * h;
            var ly = h - (_gpuLoadSamples[idx] / 100.0) * h;

            pPoints.Add(new Point(x, py));
            lPoints.Add(new Point(x, ly));
        }

        GpuPowerGraphPoints = pPoints;
        GpuLoadGraphPoints = lPoints;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
