using HSMonitor.ViewModels.Settings;

namespace HSMonitor.ViewModels.Framework;

public interface IViewModelFactory
{
    DashboardViewModel CreateDashboardViewModel();

    MessageBoxViewModel CreateMessageBoxViewModel();

    SettingsViewModel CreateSettingsViewModel();
}