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
        OpenUrl.Open(App.GitHubProjectUrl);
    }
}