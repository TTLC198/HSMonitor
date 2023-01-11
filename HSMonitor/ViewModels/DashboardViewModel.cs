using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using HSMonitor.Models;
using HSMonitor.Services;

namespace HSMonitor.ViewModels;

public class DashboardViewModel : INotifyPropertyChanged
{
    private CpuInformation _cpu;
    private GpuInformation _gpu;
    private MemoryInformation _memory;
    public CpuInformation Cpu
    {
        get => _cpu;
        set
        {
            _cpu = value;
            OnPropertyChanged();
        }
    } 

    public GpuInformation Gpu 
    {
        get => _gpu;
        set
        {
            _gpu = value;
            OnPropertyChanged();
        }
    } 
    public MemoryInformation Memory
    {
        get => _memory;
        set
        {
            _memory = value;
            OnPropertyChanged();
        }
    }

    public GpuFan GpuFan1
    {
        get => _gpu.GpuFans.ToArray()[0];
        set
        {
            _gpu.GpuFans.ToArray()[0] = value;
            OnPropertyChanged();
        }
    }
    public GpuFan GpuFan2
    {
        get => _gpu.GpuFans.ToArray()[1];
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
    
    public void OnViewLoaded()
    {
        //TODO
        HardwareInformationUpdated();
    }

    private void HardwareInformationUpdated()
    {
        Cpu = HardwareMonitorService.Cpu;
        Gpu = HardwareMonitorService.Gpu;
        Memory = HardwareMonitorService.Memory;
    }
    
    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}