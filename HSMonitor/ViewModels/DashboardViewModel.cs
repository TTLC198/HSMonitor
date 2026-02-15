﻿using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using HSMonitor.Models;
using HSMonitor.Services;

namespace HSMonitor.ViewModels;

public class DashboardViewModel : INotifyPropertyChanged
{
    private readonly SettingsService _settingsService;
    private readonly HardwareMonitorService _hardwareMonitorService;

    private CpuInformation _cpu = null!;
    private GpuInformation _gpu = null!;
    private MemoryInformation _memory = null!;

    private double _displayOpacity = 1;

    private string _cpuOcMarqueeText = "+0MHz";
    private string _gpuOcMarqueeText = "+0MHz";

    private bool _isCpuBoostActive;
    private bool _isGpuBoostActive;

    private double _cpuBoostDeltaMHz;
    private double _gpuBoostDeltaMHz;

    // GPU graphs (under POWER)
    private const int GraphSamples = 60;
    private readonly double[] _gpuPowerSamples = new double[GraphSamples];
    private readonly double[] _gpuLoadSamples = new double[GraphSamples];
    private int _gpuGraphHead;

    private PointCollection _gpuPowerGraphPoints = new();
    private PointCollection _gpuLoadGraphPoints = new();

    private ImageSource _cpuImageSource =
        new BitmapImage(new Uri(@"pack://application:,,,/HSMonitor;component/Resources/Images/UnknownLogo.png", UriKind.Absolute));

    private ImageSource _gpuImageSource =
        new BitmapImage(new Uri(@"pack://application:,,,/HSMonitor;component/Resources/Images/UnknownLogo.png", UriKind.Absolute));

    private ImageSource _memoryImageSource =
        new BitmapImage(new Uri(@"pack://application:,,,/HSMonitor;component/Resources/Images/DefaultRam.png", UriKind.Absolute));

    public ImageSource CpuImageSource
    {
        get => _cpuImageSource;
        private set { _cpuImageSource = value; OnPropertyChanged(); }
    }

    public ImageSource GpuImageSource
    {
        get => _gpuImageSource;
        private set { _gpuImageSource = value; OnPropertyChanged(); }
    }

    public ImageSource MemoryImageSource
    {
        get => _memoryImageSource;
        private set { _memoryImageSource = value; OnPropertyChanged(); }
    }

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

    public GpuFan GpuFan1
    {
        get => _gpu is { GpuFans: null } ? new GpuFan() : _gpu.GpuFans.ToArray().ElementAtOrDefault(0) ?? new GpuFan();
        set
        {
            if (_gpu is { GpuFans: null } || _gpu.GpuFans.ToArray().ElementAtOrDefault(0) is null) return;
            _gpu.GpuFans.ToArray()[0] = value;
            OnPropertyChanged();
        }
    }

    public GpuFan GpuFan2
    {
        get => _gpu is { GpuFans: null } ? new GpuFan() : _gpu.GpuFans.ToArray().ElementAtOrDefault(1) ?? new GpuFan();
        set
        {
            if (_gpu is { GpuFans: null } || _gpu.GpuFans.ToArray().ElementAtOrDefault(1) is null) return;
            _gpu.GpuFans.ToArray()[1] = value;
            OnPropertyChanged();
        }
    }

    public double DisplayOpacity
    {
        get => _displayOpacity;
        set { _displayOpacity = value; OnPropertyChanged(); }
    }

    // ===== OC / BOOST ribbons =====
    public string CpuOcMarqueeText
    {
        get => _cpuOcMarqueeText;
        set { _cpuOcMarqueeText = value; OnPropertyChanged(); }
    }

    public string GpuOcMarqueeText
    {
        get => _gpuOcMarqueeText;
        set { _gpuOcMarqueeText = value; OnPropertyChanged(); }
    }

    public bool IsCpuBoostActive
    {
        get => _isCpuBoostActive;
        set { _isCpuBoostActive = value; OnPropertyChanged(); }
    }

    public bool IsGpuBoostActive
    {
        get => _isGpuBoostActive;
        set { _isGpuBoostActive = value; OnPropertyChanged(); }
    }

    /// <summary>Delta in MHz, used for BOOST ribbon text.</summary>
    public double CpuBoostDeltaMHz
    {
        get => _cpuBoostDeltaMHz;
        set { _cpuBoostDeltaMHz = value; OnPropertyChanged(); OnPropertyChanged(nameof(CpuBoostMarqueeText)); }
    }

    public double GpuBoostDeltaMHz
    {
        get => _gpuBoostDeltaMHz;
        set { _gpuBoostDeltaMHz = value; OnPropertyChanged(); OnPropertyChanged(nameof(GpuBoostMarqueeText)); }
    }

    public string CpuBoostMarqueeText => $"BOOST +{CpuBoostDeltaMHz:0}MHz";
    public string GpuBoostMarqueeText => $"BOOST +{GpuBoostDeltaMHz:0}MHz";

    // ===== GPU graphs =====
    public PointCollection GpuPowerGraphPoints
    {
        get => _gpuPowerGraphPoints;
        private set { _gpuPowerGraphPoints = value; OnPropertyChanged(); }
    }

    public PointCollection GpuLoadGraphPoints
    {
        get => _gpuLoadGraphPoints;
        private set { _gpuLoadGraphPoints = value; OnPropertyChanged(); }
    }

    public DashboardViewModel()
    {
        
    }
    
    public DashboardViewModel(HardwareMonitorService hardwareMonitorService, SettingsService settingsService)
    {
        _hardwareMonitorService = hardwareMonitorService;
        _settingsService = settingsService;

        hardwareMonitorService.HardwareInformationUpdated += (_, _) => HardwareInformationUpdated();
        settingsService.SettingsSaved += UpdateImageFromSettings;

        HardwareInformationUpdated();
    }

    private void HardwareInformationUpdated()
    {
        Cpu = _hardwareMonitorService.Cpu;
        Gpu = _hardwareMonitorService.Gpu;
        Memory = _hardwareMonitorService.Memory;

        if (Cpu.Name is { Length: > 23 })
            Cpu.Name = Cpu.Name[..23];
        if (Gpu.Name is { Length: > 23 })
            Gpu.Name = Gpu.Name[..23];

        // "+0MHz" placeholders are always present.
        // To enable BOOST mode: set IsCpuBoostActive/IsGpuBoostActive and *BoostDeltaMHz.
        if (string.IsNullOrWhiteSpace(CpuOcMarqueeText)) 
            CpuOcMarqueeText = "+0MHz";
        if (string.IsNullOrWhiteSpace(GpuOcMarqueeText)) 
            GpuOcMarqueeText = "+0MHz";

        UpdateGpuGraphs();

        if (_settingsService is { Settings: null }) return;
        if (_settingsService.Settings.IsAutoDetectHardwareEnabled)
            UpdateImageFromComputer();
        else
            UpdateImageFromSettings(_settingsService, EventArgs.Empty);
    }

    private void UpdateImageFromComputer()
    {
        if (_cpu is { Type: not null } and not null) UpdateCpuImage(_cpu.Type);
        if (_gpu is { Type: not null } and not null) UpdateGpuImage(_gpu.Type);
        if (_memory is { Type: not null } and not null) UpdateRamImage(_memory.Type);
    }

    private void UpdateImageFromSettings(object? sender, EventArgs eventArgs)
    {
        if (sender is not SettingsService settingsService) return;
        DisplayOpacity = (double)settingsService.Settings.DeviceDisplayBrightness / 100;

        if (settingsService is { Settings.IsAutoDetectHardwareEnabled: true } or null) return;

        if (settingsService.Settings.CpuCustomType is not null) UpdateCpuImage(settingsService.Settings.CpuCustomType);
        if (settingsService.Settings.GpuCustomType is not null) UpdateGpuImage(settingsService.Settings.GpuCustomType);
        if (settingsService.Settings.MemoryCustomType is not null) UpdateRamImage(settingsService.Settings.MemoryCustomType);
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
        // The XAML graph area is 110x32.
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

    private static double ParseDouble(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return 0;
        s = s.Trim();

        s = s.Replace("W", "", StringComparison.OrdinalIgnoreCase)
             .Replace("%", "", StringComparison.OrdinalIgnoreCase)
             .Replace("MHz", "", StringComparison.OrdinalIgnoreCase)
             .Trim();

        if (double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var v)) return v;
        if (double.TryParse(s, NumberStyles.Float, CultureInfo.CurrentCulture, out v)) return v;
        return 0;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
