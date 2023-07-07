using MaterialDesignThemes.Wpf;
using Stylet;

namespace HSMonitor.ViewModels.Framework;

public abstract class DialogScreen<T> : PropertyChangedBase
{
    public T? DialogResult { get; private set; }

    public void Close(T dialogResult)
    {
        DialogResult = dialogResult;
        DialogHost.CloseDialogCommand.Execute(null,null); //close DialogHost screen
    }
}

public abstract class DialogScreen : DialogScreen<bool?>
{
}