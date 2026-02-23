namespace HSMonitor.ViewModels.Framework.Dialog;

public interface IOpenInOwnWindowDialog
{
  string Title { get; }
  double Width { get; }
  double MinWidth { get; }
  double MaxHeight { get; }
  double MinHeight { get; }
}
