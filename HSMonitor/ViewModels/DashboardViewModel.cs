using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using HSMonitor.Models;
using HSMonitor.Services;

namespace HSMonitor.ViewModels;

public class DashboardViewModel : INotifyPropertyChanged
{
    private CpuInformation _cpu = null!;
    private GpuInformation _gpu = null!;
    private MemoryInformation _memory = null!;

    private ImageSource _cpuImageSource =
        new BitmapImage(new Uri(@"../Resources/Images/AmdLogo.png", UriKind.Relative));

    private ImageSource _gpuImageSource =
        new BitmapImage(new Uri(@"../Resources/Images/NvidiaLogo.png", UriKind.Relative));

    private ImageSource _memoryImageSource =
        new BitmapImage(new Uri("../Resources/Images/RamImage.png", UriKind.Relative));

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
        get => _gpu is {GpuFans: null} or null ? new GpuFan() : _gpu.GpuFans.ToArray()[0];
        set
        {
            _gpu.GpuFans.ToArray()[0] = value;
            OnPropertyChanged();
        }
    }

    public GpuFan GpuFan2
    {
        get => _gpu is {GpuFans: null} or null ? new GpuFan() : _gpu.GpuFans.ToArray()[1];
        set
        {
            _gpu.GpuFans.ToArray()[1] = value;
            OnPropertyChanged();
        }
    }

    public DashboardViewModel(HardwareMonitorService hardwareMonitorService)
    {
        hardwareMonitorService.HardwareInformationUpdated += (_, _) => HardwareInformationUpdated();
    }

    private void HardwareInformationUpdated()
    {
        Cpu = HardwareMonitorService.Cpu;
        Gpu = HardwareMonitorService.Gpu;
        Memory = HardwareMonitorService.Memory;
        if (_cpu is {Type: not null} and not null)
            CpuImageSource = Cpu.Type switch
            {
                var amd when amd!.Contains("Amd") => new BitmapImage(
                    new Uri(@"../Resources/Images/AmdLogo.png",
                    UriKind.Relative)),
                var intel when intel!.Contains("Intel") => new BitmapImage(
                    new Uri(@"../Resources/Images/IntelLogo.png",
                    UriKind.Relative)),
                _ => new BitmapImage(
                    new Uri(@"../Resources/Images/UnknownLogo.png", UriKind.Relative))
            };
        if (_gpu is {Type: not null} and not null)
            GpuImageSource = Gpu.Type switch
            {
                var radeon when radeon!.Contains("Amd") => new BitmapImage(
                    new Uri(@"../Resources/Images/RadeonLogo.png", UriKind.Relative)),
                var intel when intel!.Contains("Intel") => new BitmapImage(
                    new Uri(@"../Resources/Images/IntelLogo.png",
                    UriKind.Relative)),
                var nvidia when nvidia!.Contains("Nvidia") => new BitmapImage(
                    new Uri(@"../Resources/Images/NvidiaLogo.png", UriKind.Relative)),
                _ => new BitmapImage(
                    new Uri(@"../Resources/Images/UnknownLogo.png", UriKind.Relative)),
            };
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}