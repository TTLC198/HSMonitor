using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

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

    private double _distance;
    private double _offset;
    private bool _running;
    private TimeSpan _lastRender;

    static MarqueeText()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(MarqueeText),
            new FrameworkPropertyMetadata(typeof(MarqueeText)));
    }

    public MarqueeText()
    {
        Loaded += (_, _) => UpdateLayoutMetrics();
        Unloaded += (_, _) => Stop();
        SizeChanged += (_, _) => UpdateLayoutMetrics();
    }

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        _canvas = GetTemplateChild(PartCanvas) as Canvas;
        _text1 = GetTemplateChild(PartText1) as TextBlock;
        _text2 = GetTemplateChild(PartText2) as TextBlock;

        if (_canvas != null)
        {
            _translate = new TranslateTransform();
            _canvas.RenderTransform = _translate;
        }

        UpdateLayoutMetrics();
    }

    #region DP

    public static readonly DependencyProperty TextProperty =
        DependencyProperty.Register(
            nameof(Text),
            typeof(string),
            typeof(MarqueeText),
            new PropertyMetadata(string.Empty, OnChanged));

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public static readonly DependencyProperty SpeedProperty =
        DependencyProperty.Register(
            nameof(Speed),
            typeof(double),
            typeof(MarqueeText),
            new PropertyMetadata(55d));

    public double Speed
    {
        get => (double)GetValue(SpeedProperty);
        set => SetValue(SpeedProperty, value);
    }

    public static readonly DependencyProperty GapProperty =
        DependencyProperty.Register(
            nameof(Gap),
            typeof(double),
            typeof(MarqueeText),
            new PropertyMetadata(20d, OnChanged));

    public double Gap
    {
        get => (double)GetValue(GapProperty);
        set => SetValue(GapProperty, value);
    }

    private static void OnChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        ((MarqueeText)d).UpdateLayoutMetrics();
    }

    #endregion

    private void UpdateLayoutMetrics()
    {
        if (!IsLoaded || _text1 == null || _text2 == null || _translate == null)
            return;

        _text1.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        double width = _text1.DesiredSize.Width;

        if (width <= 0 || ActualWidth <= 0)
            return;

        if (width <= ActualWidth)
        {
            Stop();
            Canvas.SetLeft(_text1, 0);
            Canvas.SetLeft(_text2, 0);
            _translate.X = 0;
            return;
        }

        _distance = width + Gap;

        Canvas.SetLeft(_text1, 0);
        Canvas.SetLeft(_text2, _distance);

        Start();
    }

    private void Start()
    {
        if (_running)
            return;

        _offset = 0;
        _lastRender = TimeSpan.Zero;

        CompositionTarget.Rendering += OnRendering;
        _running = true;
    }

    private void Stop()
    {
        if (!_running)
            return;

        CompositionTarget.Rendering -= OnRendering;
        _running = false;
    }

    private void OnRendering(object? sender, EventArgs e)
    {
        if (_translate == null)
            return;

        var args = (RenderingEventArgs)e;

        if (_lastRender == TimeSpan.Zero)
        {
            _lastRender = args.RenderingTime;
            return;
        }

        double delta = (args.RenderingTime - _lastRender).TotalSeconds;
        _lastRender = args.RenderingTime;

        _offset -= Speed * delta;

        if (_offset <= -_distance)
            _offset += _distance;

        _translate.X = _offset;
    }
}