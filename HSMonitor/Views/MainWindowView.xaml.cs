using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

#pragma warning disable CA1416
namespace HSMonitor.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindowView : Window
    {
        private bool _inResize;
        private double _aspect;
        
        public MainWindowView()
        {
            InitializeComponent();
            
            RenderOptions.SetBitmapScalingMode(DashboardImage, BitmapScalingMode.LowQuality);
            
            Loaded += (_, __) => _aspect = ActualWidth / ActualHeight;
            SizeChanged += OnSizeChanged;
        }
        
        private void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (_inResize || _aspect <= 0) return;

            _inResize = true;
            try
            {
                var dw = Math.Abs(e.NewSize.Width - e.PreviousSize.Width);
                var dh = Math.Abs(e.NewSize.Height - e.PreviousSize.Height);

                if (dw >= dh)
                {
                    Height = Width / _aspect;
                }
                else
                {
                    Width = Height * _aspect;
                }
            }
            finally
            {
                _inResize = false;
            }
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
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
        
        private void Show_OnClick(object sender, RoutedEventArgs e) => Show();
        private void Hide_OnClick(object sender, RoutedEventArgs e) => Hide();
        private void Close_OnClick(object sender, RoutedEventArgs e) => Application.Current.Shutdown();
    }
}