using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using HSMonitor.ViewModels.Settings;

namespace HSMonitor.Views.Settings;

public partial class SettingsView : Window
{
    private readonly Window _owner;
    private readonly double _gap;

    private bool _firstRenderSynced;

    public SettingsView(Window owner, SettingsViewModel viewModel, double gap = 10)
    {
        InitializeComponent();
        
        DataContext = viewModel;

        _owner = owner ?? throw new ArgumentNullException(nameof(owner));
        _gap = gap;

        Owner = _owner;
        ShowInTaskbar = false;

        // Чтобы не было “прыжка” при первом показе:
        // окно будет показываться прозрачным, пока не синхронизируем позицию/размер
        Opacity = 0;

        // Следим за всем, что может означать “в трей / спрятали / свернули / закрыли”
        _owner.LocationChanged += Owner_OnChanged;
        _owner.SizeChanged += Owner_OnChanged;
        _owner.StateChanged += Owner_OnChanged;
        _owner.IsVisibleChanged += Owner_OnIsVisibleChanged;
        _owner.Closed += Owner_OnClosed;

        // Первичная синхронизация максимально “поздно” в рендере (когда размеры уже корректны)
        Loaded += (_, _) =>
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                SyncToOwner(forceShow: true);
                Opacity = 1;
                _firstRenderSynced = true;
            }), DispatcherPriority.Render);
        };

        // Иногда WPF всё равно может сначала выдать 0/NaN — на всякий случай
        ContentRendered += (_, _) =>
        {
            if (!_firstRenderSynced)
            {
                SyncToOwner(forceShow: true);
                Opacity = 1;
                _firstRenderSynced = true;
            }
        };

        Closed += (_, _) => Unsubscribe();
        
        SyncToOwner(forceShow: false);
    }
    
    private void HeaderBorder_OnMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
            _owner.DragMove();
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

    private void Owner_OnClosed(object? sender, EventArgs e)
    {
        SafeClose();
    }

    private void Owner_OnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (_owner.IsVisible == false)
            SafeClose();
        else
            SyncToOwner(forceShow: true);
    }

    private void Owner_OnChanged(object? sender, EventArgs e)
    {
        if (_owner.WindowState == WindowState.Minimized)
        {
            SafeClose();
            return;
        }

        if (!_owner.IsVisible)
        {
            SafeClose();
            return;
        }

        SyncToOwner(forceShow: true);
    }

    private void SyncToOwner(bool forceShow)
    {
        if (_owner.WindowState == WindowState.Minimized || !_owner.IsVisible)
            return;

        Top = _owner.Top;
        Height = _owner.ActualHeight > 0 ? _owner.ActualHeight : _owner.Height;

        var desiredLeft = _owner.Left + (_owner.ActualWidth > 0 ? _owner.ActualWidth : _owner.Width) + _gap;

        Left = ClampToWorkArea(desiredLeft, Top).left;
        Top  = ClampToWorkArea(Left, Top).top;

        if (forceShow && !IsVisible)
            Show();
    }

    private (double left, double top) ClampToWorkArea(double left, double top)
    {
        var wa = SystemParameters.WorkArea;

        var windowWidth = ActualWidth;
        if (double.IsNaN(windowWidth) || windowWidth <= 0) windowWidth = Width;

        var windowHeight = ActualHeight;
        if (double.IsNaN(windowHeight) || windowHeight <= 0) windowHeight = Height;

        var maxLeft = wa.Right - windowWidth;
        var maxTop  = wa.Bottom - windowHeight;

        var clampedLeft = Math.Min(Math.Max(left, wa.Left), maxLeft);
        var clampedTop  = Math.Min(Math.Max(top, wa.Top), maxTop);

        return (clampedLeft, clampedTop);
    }

    private void SafeClose()
    {
        try
        {
            if (!IsLoaded)
                return;

            Close();
        }
        catch
        {
            // игнорируем редкие случаи race-condition при закрытии из событий
        }
    }

    private void Unsubscribe()
    {
        _owner.LocationChanged -= Owner_OnChanged;
        _owner.SizeChanged -= Owner_OnChanged;
        _owner.StateChanged -= Owner_OnChanged;
        _owner.IsVisibleChanged -= Owner_OnIsVisibleChanged;
        _owner.Closed -= Owner_OnClosed;
    }
}