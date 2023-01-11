using System;
using Stylet;

namespace HSMonitor.ViewModels.Framework;

//COPIED FROM https://github.com/Tyrrrz/LightBulb/blob/master/LightBulb/ViewModels/Framework/DialogScreen.cs

public abstract class DialogScreen<T> : PropertyChangedBase
{
    public T? DialogResult { get; private set; }

    public event EventHandler? Closed;

    public void Close(T dialogResult)
    {
        DialogResult = dialogResult;
        Closed?.Invoke(this, EventArgs.Empty);
    }
}

public abstract class DialogScreen : DialogScreen<bool?>
{
}