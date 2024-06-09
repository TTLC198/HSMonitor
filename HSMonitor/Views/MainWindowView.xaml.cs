using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

#pragma warning disable CA1416
namespace HSMonitor.Views;

/// <summary>
///     Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindowView : Window
{
    public MainWindowView()
    {
        InitializeComponent();

        RenderOptions.SetBitmapScalingMode(DashboardImage, BitmapScalingMode.LowQuality);
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        //clean up notifyicon (would otherwise stay open until application finishes)
        MainTaskbarIcon.Dispose();
        base.OnClosing(e);
    }

    private void MainTaskbarIcon_OnTrayBalloonTipClicked(object sender, RoutedEventArgs e)
    {
        Visibility =
            Visibility == Visibility.Visible
                ? Visibility.Hidden
                : Visibility.Visible;
    }

    private void HeaderBorder_OnMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
            DragMove();
    }

    private void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
    {
        if (App.IsHiddenOnLaunch)
            Hide();
    }

    private void Show_OnClick(object sender, RoutedEventArgs e)
    {
        Show();
    }

    private void Hide_OnClick(object sender, RoutedEventArgs e)
    {
        Hide();
    }

    private void Close_OnClick(object sender, RoutedEventArgs e)
    {
        Application.Current.Shutdown();
    }
}