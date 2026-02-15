using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace HSMonitor.Views;

public partial class DashboardView
{
    public DashboardView()
    {
        InitializeComponent();
    }
}

public sealed class MarqueeText : Control
{
    private const string PartCanvas = "PART_Canvas";
    private const string PartText1 = "PART_Text1";
    private const string PartText2 = "PART_Text2";

    private Canvas? _canvas;
    private TextBlock? _text1;
    private TextBlock? _text2;
    private TranslateTransform? _translate;
    private Storyboard? _storyboard;

    static MarqueeText()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(MarqueeText), new FrameworkPropertyMetadata(typeof(MarqueeText)));
    }

    public MarqueeText()
    {
        Loaded += (_, _) => Restart();
        Unloaded += (_, _) => Stop();
    }

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        _canvas = GetTemplateChild(PartCanvas) as Canvas;
        _text1 = GetTemplateChild(PartText1) as TextBlock;
        _text2 = GetTemplateChild(PartText2) as TextBlock;

        if (_canvas is not null)
        {
            _translate = new TranslateTransform();
            _canvas.RenderTransform = _translate;
        }

        Restart();
    }

    public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
        nameof(Text),
        typeof(string),
        typeof(MarqueeText),
        new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.AffectsRender));

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    /// <summary>Pixels per second.</summary>
    public static readonly DependencyProperty SpeedProperty = DependencyProperty.Register(
        nameof(Speed),
        typeof(double),
        typeof(MarqueeText),
        new FrameworkPropertyMetadata(55d));

    public double Speed
    {
        get => (double)GetValue(SpeedProperty);
        set => SetValue(SpeedProperty, value);
    }

    /// <summary>Gap between the two copies of the text.</summary>
    public static readonly DependencyProperty GapProperty = DependencyProperty.Register(
        nameof(Gap),
        typeof(double),
        typeof(MarqueeText),
        new FrameworkPropertyMetadata(18d));

    public double Gap
    {
        get => (double)GetValue(GapProperty);
        set => SetValue(GapProperty, value);
    }

    protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);
        if (e.Property == TextProperty || e.Property == SpeedProperty || e.Property == GapProperty)
            Restart();
    }

    private void Stop()
    {
        _storyboard?.Stop(this);
        _storyboard = null;
        if (_translate is not null) _translate.X = 0;
    }

    private void Restart()
    {
        if (!IsLoaded)
            return;

        if (_canvas is null || _text1 is null || _text2 is null || _translate is null)
            return;

        Stop();

        _text1.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        var w = Math.Max(1, _text1.DesiredSize.Width);

        Canvas.SetLeft(_text1, 0);
        Canvas.SetTop(_text1, 0);
        Canvas.SetLeft(_text2, w + Gap);
        Canvas.SetTop(_text2, 0);

        // If text fits inside viewport - no animation.
        var viewport = ActualWidth;
        if (double.IsNaN(viewport) || viewport <= 0)
            viewport = Width;

        if (viewport > 0 && w + Gap <= viewport)
            return;

        var distance = w + Gap;
        var pixelsPerSecond = Math.Max(10, Speed);
        var seconds = distance / pixelsPerSecond;

        var anim = new DoubleAnimation
        {
            From = 0,
            To = -distance,
            Duration = TimeSpan.FromSeconds(seconds),
            RepeatBehavior = RepeatBehavior.Forever
        };

        _storyboard = new Storyboard();
        _storyboard.Children.Add(anim);
        Storyboard.SetTarget(anim, _translate);
        Storyboard.SetTargetProperty(anim, new PropertyPath(TranslateTransform.XProperty));

        _storyboard.Begin(this, true);
    }
}

/// <summary>Visibility converter with optional inversion: ConverterParameter="invert".</summary>
public sealed class BooleanToVisibilityConverterEx : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var b = value is bool bb && bb;
        if (parameter is string s && s.Equals("invert", StringComparison.OrdinalIgnoreCase))
            b = !b;

        return b ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is Visibility v)
        {
            var b = v == Visibility.Visible;
            if (parameter is string s && s.Equals("invert", StringComparison.OrdinalIgnoreCase))
                b = !b;
            return b;
        }

        return false;
    }
}
