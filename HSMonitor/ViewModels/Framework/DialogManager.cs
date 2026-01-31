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

public sealed class DialogManager : IDisposable
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

        var view = _viewManager.CreateAndBindViewForModelIfNecessary(dialogScreen);

        // Warm-up the view and trigger all bindings (nested ContentControls etc.).
        view.Arrange(new Rect(0, 0, 500, 500));

        return _dialogScreenViewCache[dialogScreenType] = view;
    }

    public async Task<T?> ShowDialogAsync<T>(DialogScreen<T> dialogScreen)
    {
        var view = GetViewForDialogScreen(dialogScreen);

        await _dialogLock.WaitAsync().ConfigureAwait(false);
        try
        {
            // Always show dialogs on UI thread
            if (Application.Current?.Dispatcher is null)
            {
                // Fallback (should not really happen in WPF app)
                await DialogHost.Show(view).ConfigureAwait(false);
                return dialogScreen.DialogResult;
            }

            return await Application.Current.Dispatcher.InvokeAsync(async () =>
            {
                // Wide / separate-window dialog route
                if (dialogScreen is IOpenInOwnWindowDialog ownWindow)
                {
                    var wnd = new DialogHostWindow
                    {
                        Title = string.IsNullOrWhiteSpace(ownWindow.Title) ? "Диалог" : ownWindow.Title,
                        Width = ownWindow.Width > 0 ? ownWindow.Width : 900,
                        MinWidth = ownWindow.MinWidth > 0 ? ownWindow.MinWidth : 720,
                    };

                    // Optional sizes
                    if (ownWindow.Height is var h and > 0)
                        wnd.Height = h;
                    if (ownWindow.MinHeight is var mh and > 0)
                        wnd.MinHeight = mh;

                    wnd.Owner = Application.Current.MainWindow;

                    // Show window and host the dialog inside its DialogHost
                    wnd.Show();
                    try
                    {
                        // IMPORTANT: show inside the window's DialogHost, not the global one
                        await wnd.DialogHostControl.ShowDialog(view);
                    }
                    finally
                    {
                        wnd.Close();
                    }

                    return dialogScreen.DialogResult;
                }

                // Default route: in main window DialogHost
                await DialogHost.Show(view);
                return dialogScreen.DialogResult;

            }).Task.Unwrap().ConfigureAwait(false);
        }
        finally
        {
            _dialogLock.Release();
        }
    }

    public void Dispose() => _dialogLock.Dispose();
}