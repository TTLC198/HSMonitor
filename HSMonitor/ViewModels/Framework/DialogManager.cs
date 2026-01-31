using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using HSMonitor.ViewModels.Framework.Dialog;
using HSMonitor.Views;
using MaterialDesignThemes.Wpf;
using Stylet;

namespace HSMonitor.ViewModels.Framework;

public class DialogManager : IDisposable
{
    private readonly IViewManager _viewManager;

    // Cache and re-use dialog screen views, as creating them is incredibly slow
    private readonly Dictionary<Type, UIElement> _dialogScreenViewCache = new();
    private readonly SemaphoreSlim _dialogLock = new(1, 1);

    public DialogManager(IViewManager viewManager)
    {
        _viewManager = viewManager;
    }

    public UIElement GetViewForDialogScreen<T>(DialogScreen<T> dialogScreen)
    {
        var dialogScreenType = dialogScreen.GetType();
        if (_dialogScreenViewCache.TryGetValue(dialogScreenType, out var cachedView))
        {
            _viewManager.BindViewToModel(cachedView, dialogScreen);
            return cachedView;
        }
        else
        {
            var view = _viewManager.CreateAndBindViewForModelIfNecessary(dialogScreen);

            // This warms up the view and triggers all bindings.
            // We need to do this, as the view may have nested model-bound ContentControls
            // which take a very long time to load.
            // By pre-loading them as early as possible, we avoid doing it when the dialog
            // actually pops up, which improves user experience.
            // Ideally, the whole view cache should be populated at application startup.
            view.Arrange(new Rect(0, 0, 500, 500));

            return _dialogScreenViewCache[dialogScreenType] = view;
        }
    }

    public async Task<T?> ShowDialogAsync<T>(DialogScreen<T> dialogScreen)
    {
        var view = GetViewForDialogScreen(dialogScreen);

        await _dialogLock.WaitAsync();
        try
        {
            if (dialogScreen is IOpenInOwnWindowDialog ownWindow)
            {
                var wnd = new DialogHostWindow
                {
                    Title = ownWindow.Title,
                    Width = ownWindow.Width,
                    MinWidth = ownWindow.MinWidth,
                };

                if (ownWindow.MinHeight is double mh) wnd.MinHeight = mh;
                if (ownWindow.Height is double h) wnd.Height = h;

                wnd.Owner = Application.Current?.MainWindow;

                wnd.Show();

                await wnd.RootDialogHost.ShowDialog(view);

                wnd.Close();
                return dialogScreen.DialogResult;
            }

            await DialogHost.Show(view);
            return dialogScreen.DialogResult;
        }
        finally
        {
            _dialogLock.Release();
        }
    }


    public void Dispose() => _dialogLock.Dispose();
}