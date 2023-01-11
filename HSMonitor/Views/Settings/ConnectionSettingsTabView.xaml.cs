using System.Text.RegularExpressions;
using System.Windows.Controls;
using System.Windows.Input;

namespace HSMonitor.Views.Settings;

public partial class ConnectionSettingsTabView : UserControl
{
    public ConnectionSettingsTabView()
    {
        InitializeComponent();
    }
    
    private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
    {
        var regex = new Regex("[^0-9]+");
        e.Handled = regex.IsMatch(e.Text);
    }
}