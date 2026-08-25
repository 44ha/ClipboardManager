using System.Windows.Interop;

namespace ClipboardManager.Windows.Native;

/// <summary>
/// Manages a lightweight, invisible Win32 window (via WPF HwndSource) to receive
/// WM_CLIPBOARDUPDATE and WM_HOTKEY window messages from the operating system.
/// </summary>
public sealed class MessageWindow : IDisposable
{
    private HwndSource? _hwndSource;
    private readonly HwndSourceHook _hook;
    private bool _disposed;

    public IntPtr Handle => _hwndSource?.Handle ?? IntPtr.Zero;

    public MessageWindow(HwndSourceHook hook)
    {
        _hook = hook ?? throw new ArgumentNullException(nameof(hook));

        var parameters = new HwndSourceParameters("ClipboardManager_MessageWindow")
        {
            Width = 0,
            Height = 0,
            PositionX = 0,
            PositionY = 0,
            WindowStyle = 0
        };

        _hwndSource = new HwndSource(parameters);
        _hwndSource.AddHook(_hook);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        if (_hwndSource != null)
        {
            _hwndSource.RemoveHook(_hook);
            _hwndSource.Dispose();
            _hwndSource = null;
        }
    }
}
