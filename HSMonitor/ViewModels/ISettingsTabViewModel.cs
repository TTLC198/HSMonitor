namespace HSMonitor.ViewModels;

public interface ISettingsTabViewModel
{
    int Order { get; }
    string Name { get; }
    bool IsSelected { get; set; }
}