using System.Text.RegularExpressions;
using System.Windows.Controls;
using System.Windows.Input;

namespace HSMonitor.Views.Settings;

public partial class HardwareSettingsTabView : UserControl
{
    public HardwareSettingsTabView()
    {
        InitializeComponent();
    }
    
    private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
    {
        var regex = new Regex("[^0-9]+");
        e.Handled = regex.IsMatch(e.Text) && e.Text.Length <= 4;
    }
}