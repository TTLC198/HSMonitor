using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using HSMonitor.Utils;

namespace HSMonitor.Views.Settings;

public partial class UpdateSettingsTabView : UserControl
{
    public UpdateSettingsTabView()
    {
        InitializeComponent();
    }

    private void GithubLinkPageOpen(object sender, MouseButtonEventArgs e)
    {
        if (sender is Grid {Name: "GithubClientUrlGrid"})
            OpenUrl.Open(App.GitHubClientProjectUrl);
        if (sender is Grid {Name: "GithubUrlGrid"})
            OpenUrl.Open(App.GitHubProjectUrl);
    }
}