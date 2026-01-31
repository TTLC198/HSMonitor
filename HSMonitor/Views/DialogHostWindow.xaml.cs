using System.Windows;
using MaterialDesignThemes.Wpf;

namespace HSMonitor.Views;

public partial class DialogHostWindow : Window
{
  public DialogHostWindow()
  {
    InitializeComponent();
  }
  
  public DialogHost DialogHostControl => RootDialogHost;
}

