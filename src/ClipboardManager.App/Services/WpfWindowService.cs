using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using ClipboardManager.App.Views;

namespace ClipboardManager.App.Services;

/// <summary>
/// WPF implementation of IWindowService for coordinating window visibility on the UI thread.
/// </summary>
public class WpfWindowService : IWindowService
{
    private readonly IServiceProvider _serviceProvider;
    private HistoryWindow? _historyWindow;
    private SettingsWindow? _settingsWindow;

    public WpfWindowService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    public void ShowHistoryWindow()
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            EnsureHistoryWindow();
            if (_historyWindow!.WindowState == WindowState.Minimized)
            {
                _historyWindow.WindowState = WindowState.Normal;
            }
            _historyWindow.Show();
            _historyWindow.Activate();
            _historyWindow.FocusSearchBox();
            _ = (_historyWindow.DataContext as ViewModels.HistoryViewModel)?.LoadHistoryAsync();
        });
    }

    public void HideHistoryWindow()
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            _historyWindow?.Hide();
        });
    }

    public void ToggleHistoryWindow()
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            EnsureHistoryWindow();
            if (_historyWindow!.IsVisible && _historyWindow.WindowState != WindowState.Minimized)
            {
                _historyWindow.WindowState = WindowState.Minimized;
            }
            else
            {
                ShowHistoryWindow();
            }
        });
    }

    public void ShowSettingsWindow()
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            EnsureSettingsWindow();
            _settingsWindow!.Show();
            _settingsWindow.Activate();
        });
    }

    public void HideSettingsWindow()
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            _settingsWindow?.Hide();
        });
    }

    private void EnsureHistoryWindow()
    {
        if (_historyWindow == null)
        {
            _historyWindow = _serviceProvider.GetRequiredService<HistoryWindow>();
            _historyWindow.Closed += (_, _) =>
            {
                Application.Current.Shutdown();
            };
        }
    }

    private void EnsureSettingsWindow()
    {
        if (_settingsWindow == null)
        {
            _settingsWindow = _serviceProvider.GetRequiredService<SettingsWindow>();
            _settingsWindow.Closing += (_, e) =>
            {
                e.Cancel = true;
                _settingsWindow.Hide();
            };
        }
    }
}
