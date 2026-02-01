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
            if (Application.Current?.Dispatcher is null)
            {
                await DialogHost.Show(view).ConfigureAwait(false);
                return dialogScreen.DialogResult;
            }

            return await Application.Current.Dispatcher.InvokeAsync(async () =>
            {
                var owner = Application.Current.MainWindow;

                var isSettings =
                    dialogScreen.GetType().Name == "SettingsViewModel" ||
                    (dialogScreen.GetType().FullName?.EndsWith(".SettingsViewModel", StringComparison.Ordinal) ?? false);

                if (isSettings && owner is not null)
                {
                    double width = 460;
                    double minWidth = 360;

                    if (dialogScreen is IOpenInOwnWindowDialog ownWindow)
                    {
                        if (ownWindow.Width > 0) width = ownWindow.Width;
                        if (ownWindow.MinWidth > 0) minWidth = ownWindow.MinWidth;
                    }

                    var wnd = new SettingsSideHostWindow(owner, gap: 10)
                    {
                        Width = width,
                        MinWidth = minWidth,
                    };

                    wnd.Show();

                    try
                    {
                        await wnd.DialogHostControl.ShowDialog(view);
                    }
                    finally
                    {
                        wnd.Close();
                    }

                    return dialogScreen.DialogResult;
                }

                if (dialogScreen is IOpenInOwnWindowDialog ownWindowDialog)
                {
                    var wnd = new DialogHostWindow
                    {
                        Title = string.IsNullOrWhiteSpace(ownWindowDialog.Title) ? "Диалог" : ownWindowDialog.Title,
                        Width = ownWindowDialog.Width > 0 ? ownWindowDialog.Width : 900,
                        MinWidth = ownWindowDialog.MinWidth > 0 ? ownWindowDialog.MinWidth : 720,
                    };

                    if (ownWindowDialog.Height is var h and > 0) wnd.Height = h;
                    if (ownWindowDialog.MinHeight is var mh and > 0) wnd.MinHeight = mh;

                    wnd.Owner = owner;
                    wnd.Show();
                    try
                    {
                        await wnd.DialogHostControl.ShowDialog(view);
                    }
                    finally
                    {
                        wnd.Close();
                    }

                    return dialogScreen.DialogResult;
                }

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