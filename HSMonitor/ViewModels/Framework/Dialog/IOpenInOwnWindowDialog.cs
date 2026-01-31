namespace HSMonitor.ViewModels.Framework.Dialog;

public interface IOpenInOwnWindowDialog
{
  string Title { get; }
  double Width { get; }
  double MinWidth { get; }
  double Height { get; }
  double MinHeight { get; }
}
