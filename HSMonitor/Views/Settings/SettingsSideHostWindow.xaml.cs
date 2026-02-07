using System.Windows;
using System.Windows.Threading;
using MaterialDesignThemes.Wpf;

namespace HSMonitor.Views;

public partial class SettingsSideHostWindow : Window
{
    private readonly Window _owner;
    private readonly double _gap;

    private bool _firstRenderSynced;

    public SettingsSideHostWindow(Window owner, double gap = 10)
    {
        InitializeComponent();

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

    public DialogHost DialogHostControl => RootDialogHost;

    private void Owner_OnClosed(object? sender, EventArgs e)
    {
        // Если основное окно закрыли — закрываем и настройки
        SafeClose();
    }

    private void Owner_OnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        // ✅ Улучшение #1: если основное окно спрятали (Hide в трей) — закрываем окно настроек
        if (_owner.IsVisible == false)
            SafeClose();
        else
            SyncToOwner(forceShow: true);
    }

    private void Owner_OnChanged(object? sender, EventArgs e)
    {
        // Если свернули — закрываем (или можно Hide(), но ты просил “только для настроек” и логично закрывать)
        if (_owner.WindowState == WindowState.Minimized)
        {
            SafeClose();
            return;
        }

        // Если основное окно стало невидимым (Hide) — закрываем
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

        // Ровно та же высота и Top
        Top = _owner.Top;
        Height = _owner.ActualHeight > 0 ? _owner.ActualHeight : _owner.Height;

        // Ставим справа + gap
        var desiredLeft = _owner.Left + (_owner.ActualWidth > 0 ? _owner.ActualWidth : _owner.Width) + _gap;

        // Важно: позиционируем ДО Show(), чтобы не было “рывка”
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

            // Если окно в процессе показа диалога — Close() корректно закрывает его вместе с DialogHost
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