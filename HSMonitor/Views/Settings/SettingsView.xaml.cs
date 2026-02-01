using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace HSMonitor.Views.Settings;

public partial class SettingsView : UserControl
{
    public SettingsView()
    {
        InitializeComponent();
    }
    
    private void OnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        // Ищем ScrollViewer в визуальном дереве и скроллим его
        if (sender is not DependencyObject d) return;

        var sv = FindChild<ScrollViewer>(d);
        if (sv == null) return;

        sv.ScrollToVerticalOffset(sv.VerticalOffset - e.Delta);
        e.Handled = true;
    }

    private static T? FindChild<T>(DependencyObject root) where T : DependencyObject
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            if (child is T t) return t;
            var res = FindChild<T>(child);
            if (res != null) return res;
        }
        return null;
    }
}