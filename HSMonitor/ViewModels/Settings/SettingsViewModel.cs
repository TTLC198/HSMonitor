using System.Collections.Generic;
using System.Linq;
using HSMonitor.Services;
using HSMonitor.ViewModels.Framework;

namespace HSMonitor.ViewModels.Settings;

public class SettingsViewModel : DialogScreen
{
    private readonly SettingsService _settingsService;

    public IReadOnlyList<ISettingsTabViewModel> Tabs { get; }

    public ISettingsTabViewModel? ActiveTab { get; private set; }

    public SettingsViewModel(SettingsService settingsService, IEnumerable<ISettingsTabViewModel> tabs)
    {
        _settingsService = settingsService;
        Tabs = tabs.OrderBy(t => t.Order).ToArray();

        // Pre-select first tab
        var firstTab = Tabs.FirstOrDefault();
        if (firstTab is not null)
            ActivateTab(firstTab);
    }

    public void ActivateTab(ISettingsTabViewModel settingsTab)
    {
        // Deactivate previously selected tab
        if (ActiveTab is not null)
            ActiveTab.IsSelected = false;
        
        ActiveTab = settingsTab;
        settingsTab.IsSelected = true;
    }
    
    public void ActivateTabByType<T>() where T : ISettingsTabViewModel
    {
        var tab = Tabs.OfType<T>().FirstOrDefault();
        if (tab is not null)
            ActivateTab(tab);
    }

    public async void Reset() => 
        await _settingsService.Reset();

    public async void Save()
    {
        await _settingsService.Save();
        Close(true);
    }

    public async void Cancel()
    {
        await _settingsService.Load();
        Close(false);
    }
}