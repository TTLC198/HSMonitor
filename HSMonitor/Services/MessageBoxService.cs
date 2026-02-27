using HSMonitor.Properties;
using HSMonitor.ViewModels;
using HSMonitor.ViewModels.Framework;

namespace HSMonitor.Services;

public class MessageBoxService(IViewModelFactory viewModelFactory, DialogManager dialogManager)
{
  public Task<bool> ShowAsync(string? title = null, string? message = null, string? okButtonText = null, string? cancelButtonText = null)
  {
    var messageBoxDialog = viewModelFactory.CreateMessageBoxViewModel(
      title: title ?? Resources.MessageBoxErrorTitle,
      message: message?.Trim() ?? Resources.MessageBoxErrorText,
      okButtonText: okButtonText ?? Resources.MessageBoxOkButtonText,
      cancelButtonText: cancelButtonText
    );
    return dialogManager.ShowDialogAsync(messageBoxDialog);
  }
}
