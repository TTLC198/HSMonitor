using HSMonitor.Services;
using Stylet;

namespace HSMonitor.ViewModels.Settings;
#pragma warning disable CA1416
public abstract class SettingsTabBaseViewModel : PropertyChangedBase, ISettingsTabViewModel
{
    public int Order { get; }

    public string Name { get; }

    public bool IsSelected { get; set; }

    protected SettingsService SettingsService { get; }

    protected SettingsTabBaseViewModel(SettingsService settingsService, int order, string name)
    {
        SettingsService = settingsService;
        Order = order;
        Name = name;

        SettingsService.SettingsReset += (_, _) => Refresh();
        SettingsService.SettingsSaved += (_, _) => Refresh();
        SettingsService.SettingsLoaded += (_, _) => Refresh();
    }
}