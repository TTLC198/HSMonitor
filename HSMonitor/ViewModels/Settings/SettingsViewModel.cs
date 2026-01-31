﻿using System.Collections.Generic;
using System.Linq;
using HSMonitor.Services;
using HSMonitor.ViewModels.Framework;
using HSMonitor.ViewModels.Framework.Dialog;

namespace HSMonitor.ViewModels.Settings;

public class SettingsViewModel : DialogScreen<bool>, IOpenInOwnWindowDialog
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

    public void NextTab()
    {
        if (Tabs.Count == 0)
            return;

        if (ActiveTab is null)
        {
            ActivateTab(Tabs[0]);
            return;
        }

        var currentIndex = -1;
        for (var i = 0; i < Tabs.Count; i++)
        {
            if (ReferenceEquals(Tabs[i], ActiveTab))
            {
                currentIndex = i;
                break;
            }
        }

        if (currentIndex < 0)
        {
            ActivateTab(Tabs[0]);
            return;
        }

        var nextIndex = (currentIndex + 1) % Tabs.Count;
        ActivateTab(Tabs[nextIndex]);
    }

    public void PrevTab()
    {
        if (Tabs.Count == 0)
            return;

        if (ActiveTab is null)
        {
            ActivateTab(Tabs[0]);
            return;
        }

        var currentIndex = -1;
        for (var i = 0; i < Tabs.Count; i++)
        {
            if (ReferenceEquals(Tabs[i], ActiveTab))
            {
                currentIndex = i;
                break;
            }
        }

        if (currentIndex < 0)
        {
            ActivateTab(Tabs[0]);
            return;
        }

        var prevIndex = (currentIndex - 1 + Tabs.Count) % Tabs.Count;
        ActivateTab(Tabs[prevIndex]);
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
        Close(true);
        await _settingsService.Save();
    }

    public async void Cancel()
    {
        await _settingsService.LoadAsync();
        Close(false);
    }

    public string Title => "Настройки";
    public double Width => 500;
    public double MinWidth => 360;
    public double Height => 400;
    public double MinHeight => 350;
}
