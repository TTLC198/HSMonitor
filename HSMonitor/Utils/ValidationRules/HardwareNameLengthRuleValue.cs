using System.Windows;

namespace HSMonitor.Utils.ValidationRules;

public class HardwareNameLengthRuleValue : DependencyObject
{
    public static readonly DependencyProperty MinBindingProperty = DependencyProperty.RegisterAttached(
        nameof(Min),
        typeof(int),
        typeof(HardwareNameLengthRuleValue),
        new PropertyMetadata(default(int))
    );

    public static readonly DependencyProperty MaxBindingProperty = DependencyProperty.RegisterAttached(
        nameof(Max),
        typeof(int),
        typeof(HardwareNameLengthRuleValue),
        new PropertyMetadata(default(int))
    );

    public int Min
    {
        get => (int) GetValue(MinBindingProperty);
        set => SetValue(MinBindingProperty, value);
    }

    public int Max
    {
        get => (int) GetValue(MaxBindingProperty);
        set => SetValue(MaxBindingProperty, value);
    }
}