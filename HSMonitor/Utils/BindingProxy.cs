using System.Windows;

namespace HSMonitor.Utils;

public class BindingProxy : System.Windows.Freezable
{
    protected override Freezable CreateInstanceCore() {
        return new BindingProxy();
    }
 
    public object Data {
        get { return (object)GetValue(DataProperty); }
        set { SetValue(DataProperty, value); }
    }
 
    public static readonly DependencyProperty DataProperty =
        DependencyProperty.Register(nameof(Data), typeof(object), typeof(BindingProxy), new PropertyMetadata(null));
}