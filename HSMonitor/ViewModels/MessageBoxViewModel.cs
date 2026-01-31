using HSMonitor.ViewModels.Framework;
using HSMonitor.ViewModels.Framework.Dialog;

namespace HSMonitor.ViewModels;

public class MessageBoxViewModel : DialogScreen<bool>, IOpenInOwnWindowDialog
{
    public string? Title { get; set; }
    public double Width => 400;
    public double MinWidth => 260;
    public double Height => 320;
    public double MinHeight => 200;

    public string? Message { get; set; }

    public bool IsOkButtonVisible { get; set; } = true;

    public string? OkButtonText { get; set; }

    public bool IsCancelButtonVisible { get; set; }

    public string? CancelButtonText { get; set; }
    
    public int ButtonsCount =>
        (IsOkButtonVisible ? 1 : 0) +
        (IsCancelButtonVisible ? 1 : 0);
}

public static class MessageBoxViewModelExtensions
{
    public static MessageBoxViewModel CreateMessageBoxViewModel(
        this IViewModelFactory factory,
        string title, string message,
        string? okButtonText, string? cancelButtonText)
    {
        var viewModel = factory.CreateMessageBoxViewModel();
        viewModel.Title = title;
        viewModel.Message = message;

        viewModel.IsOkButtonVisible = !string.IsNullOrWhiteSpace(okButtonText);
        viewModel.OkButtonText = okButtonText;
        viewModel.IsCancelButtonVisible = !string.IsNullOrWhiteSpace(cancelButtonText);
        viewModel.CancelButtonText = cancelButtonText;

        return viewModel;
    }
}