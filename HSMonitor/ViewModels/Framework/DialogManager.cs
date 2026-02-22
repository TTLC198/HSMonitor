using System.Collections.Concurrent;
using System.Windows;
using HSMonitor.Utils.Logger;
using HSMonitor.ViewModels.Framework.Dialog;
using HSMonitor.ViewModels.Settings;
using HSMonitor.Views;
using HSMonitor.Views.Settings;
using MaterialDesignThemes.Wpf;
using Stylet;

namespace HSMonitor.ViewModels.Framework;

public sealed class DialogManager : IDisposable
{
    private readonly IViewManager _viewManager;

    private readonly ConcurrentDictionary<Type, UIElement> _dialogScreenViewCache = new();

    private readonly SemaphoreSlim _dialogLock = new(1, 1);

    private DialogSession? _currentSession;
    private Action? _closeWindowFallback;

    private Window? _settingsWindow;
    
    private readonly ILogger<DialogManager> _logger;

    public DialogManager(IViewManager viewManager, ILogger<DialogManager> logger)
    {
        _viewManager = viewManager ?? throw new ArgumentNullException(nameof(viewManager));
        _logger = logger;
    }

    /// <summary>
    /// Показ диалога с результатом. Toggle: если диалог уже открыт — текущий закрывается, метод возвращает default.
    /// </summary>
    public async Task<T?> ShowDialogAsync<T>(DialogScreen<T> dialogScreen)
    {
        if (dialogScreen is null) throw new ArgumentNullException(nameof(dialogScreen));

        if (!await _dialogLock.WaitAsync(0).ConfigureAwait(false))
        {
            await CloseCurrentDialogAsync().ConfigureAwait(false);
            return default;
        }

        try
        {
            var view = await GetOrCreateViewOnUiAsync(dialogScreen).ConfigureAwait(false);

            if (Application.Current?.Dispatcher is null)
            {
                await DialogHost.Show(view).ConfigureAwait(false);
                return dialogScreen.DialogResult;
            }

            return await await Application.Current.Dispatcher.InvokeAsync(() => ShowOnUi(dialogScreen, view)).Task
                .ConfigureAwait(false);
        }
        finally
        {
            _dialogLock.Release();
        }
    }

    /// <summary>
    /// Toggle-окно настроек: если уже открыто — закрываем; иначе открываем.
    /// </summary>
    public Task ShowSettingsDialogAsync(IOpenInOwnWindowDialog settingsDialog)
    {
        if (settingsDialog is null) throw new ArgumentNullException(nameof(settingsDialog));

        if (Application.Current?.Dispatcher is null)
        {
            return Task.CompletedTask;
        }

        return Application.Current.Dispatcher.InvokeAsync(() =>
        {
            if (_settingsWindow is { } existing)
            {
                try { existing.Close(); } catch { /* ignore */ }
                _settingsWindow = null;
                return;
            }

            var owner = Application.Current.MainWindow;
            if (owner is null)
                return;

            if (settingsDialog is not SettingsViewModel settingsViewModel)
            {
                _logger.Error(null, $"SettingsDialog не является SettingsViewModel");
                return;
            }

            var (width, minWidth) = ResolveWidth(settingsDialog, fallbackWidth: 460, fallbackMinWidth: 360);

            var wnd = new SettingsView(owner, settingsViewModel, gap: 10)
            {
                Width = width,
                MinWidth = minWidth,
            };

            _settingsWindow = wnd;

            settingsViewModel.CloseRequested += (_, __) =>
            {
                try { wnd.Close(); } catch { /* ignore */ }
            };

            wnd.Closing += (_, __) =>
            {
                try { _currentSession?.Close(); } catch { /* ignore */ }

                if (ReferenceEquals(_settingsWindow, wnd))
                    _settingsWindow = null;
            };

            _closeWindowFallback = () =>
            {
                try { wnd.Close(); } catch { /* ignore */ }
            };

            wnd.Show();
        }).Task;
    }

    private async Task<UIElement> GetOrCreateViewOnUiAsync<T>(DialogScreen<T> dialogScreen)
    {
        if (Application.Current?.Dispatcher is null)
            return GetOrCreateView(dialogScreen);

        if (Application.Current.Dispatcher.CheckAccess())
            return GetOrCreateView(dialogScreen);

        return await Application.Current.Dispatcher.InvokeAsync(() => GetOrCreateView(dialogScreen)).Task
            .ConfigureAwait(false);
    }

    private UIElement GetOrCreateView<T>(DialogScreen<T> dialogScreen)
    {
        var dialogScreenType = dialogScreen.GetType();

        var view = _dialogScreenViewCache.GetOrAdd(dialogScreenType, _ =>
        {
            var created = _viewManager.CreateAndBindViewForModelIfNecessary(dialogScreen);

            created.Arrange(new Rect(0, 0, 500, 500));

            return created;
        });

        _viewManager.BindViewToModel(view, dialogScreen);

        return view;
    }

    private async Task<T?> ShowOnUi<T>(DialogScreen<T> dialogScreen, UIElement view)
    {
        var owner = Application.Current?.MainWindow;

        if (dialogScreen is IOpenInOwnWindowDialog ownWindowDialog)
        {
            var wnd = CreateDialogWindow(owner, ownWindowDialog);

            wnd.Closing += (_, __) =>
            {
                try { _currentSession?.Close(); } catch { /* ignore */ }
            };

            _closeWindowFallback = () =>
            {
                try { wnd.Close(); } catch { /* ignore */ }
            };

            wnd.Show();

            try
            {
                await wnd.DialogHostControl.ShowDialog(
                    view,
                    openedEventHandler: (_, args) => _currentSession = args.Session
                ).ConfigureAwait(true);

                return dialogScreen.DialogResult;
            }
            finally
            {
                _currentSession = null;
                _closeWindowFallback = null;
                try { wnd.Close(); } catch { /* ignore */ }
            }
        }

        try
        {
            await DialogHost.Show(
                view,
                openedEventHandler: (_, args) => _currentSession = args.Session
            ).ConfigureAwait(true);

            return dialogScreen.DialogResult;
        }
        finally
        {
            _currentSession = null;
        }
    }

    private static DialogHostWindow CreateDialogWindow(Window? owner, IOpenInOwnWindowDialog dialog)
    {
        var wnd = new DialogHostWindow
        {
            Title = string.IsNullOrWhiteSpace(dialog.Title) ? "Диалог" : dialog.Title,
            Width = dialog.Width > 0 ? dialog.Width : 900,
            MinWidth = dialog.MinWidth > 0 ? dialog.MinWidth : 720,
            Owner = owner,
        };

        if (dialog.Height > 0) wnd.Height = dialog.Height;
        if (dialog.MinHeight > 0) wnd.MinHeight = dialog.MinHeight;

        return wnd;
    }

    private static (double width, double minWidth) ResolveWidth(IOpenInOwnWindowDialog dialog, double fallbackWidth, double fallbackMinWidth)
    {
        var width = dialog.Width > 0 ? dialog.Width : fallbackWidth;
        var minWidth = dialog.MinWidth > 0 ? dialog.MinWidth : fallbackMinWidth;
        return (width, minWidth);
    }

    private Task CloseCurrentDialogAsync()
    {
        var session = _currentSession;
        var closeWindow = _closeWindowFallback;

        if (Application.Current?.Dispatcher is null)
        {
            try { session?.Close(); } catch { /* ignore */ }
            try { closeWindow?.Invoke(); } catch { /* ignore */ }
            return Task.CompletedTask;
        }

        return Application.Current.Dispatcher.InvokeAsync(() =>
        {
            try { session?.Close(); } catch { /* ignore */ }
            try { closeWindow?.Invoke(); } catch { /* ignore */ }
        }).Task;
    }

    public void Dispose()
    {
        try
        {
            // Освобождаем UI-ресурсы максимально корректно.
            _ = CloseCurrentDialogAsync();
        }
        catch
        {
            // ignore
        }

        try
        {
            if (Application.Current?.Dispatcher?.CheckAccess() == true)
            {
                try { _settingsWindow?.Close(); } catch { /* ignore */ }
                _settingsWindow = null;
            }
            else
            {
                Application.Current?.Dispatcher?.Invoke(() =>
                {
                    try { _settingsWindow?.Close(); } catch { /* ignore */ }
                    _settingsWindow = null;
                });
            }
        }
        catch
        {
            // ignore
        }

        _dialogLock.Dispose();
    }
}