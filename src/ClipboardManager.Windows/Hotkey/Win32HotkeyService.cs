using System.Runtime.InteropServices;
using ClipboardManager.Core.Models;
using ClipboardManager.Core.Services;
using ClipboardManager.Windows.Native;

namespace ClipboardManager.Windows.Hotkey;

/// <summary>
/// Registers and unregisters a global Windows hotkey via Win32 RegisterHotKey.
/// Hooks WndProc to raise HotkeyPressed when the keystroke is detected anywhere in the OS.
/// </summary>
public sealed class Win32HotkeyService : IHotkeyService
{
    private const int HotkeyId = 0x9001;

    private MessageWindow? _messageWindow;
    private HotkeyGesture? _currentHotkey;
    private bool _isRegistered;
    private bool _disposed;

    public event EventHandler? HotkeyPressed;

    public HotkeyGesture? CurrentHotkey => _currentHotkey;

    public bool IsRegistered => _isRegistered;

    public Win32HotkeyService()
    {
        _messageWindow = new MessageWindow(WndProc);
    }

    public bool Register(HotkeyGesture gesture, out string? errorMessage)
    {
        ArgumentNullException.ThrowIfNull(gesture);

        errorMessage = null;

        // Unregister any previous shortcut before registering the new one
        Unregister();

        if (_messageWindow == null || _messageWindow.Handle == IntPtr.Zero)
        {
            errorMessage = "Internal message window handle is not initialized.";
            return false;
        }

        uint fsModifiers = (uint)gesture.Modifiers;
        uint vk = (uint)gesture.VirtualKey;

        bool success = NativeMethods.RegisterHotKey(_messageWindow.Handle, HotkeyId, fsModifiers, vk);
        if (!success)
        {
            int errorCode = Marshal.GetLastWin32Error();
            if (errorCode == NativeMethods.ERROR_HOTKEY_ALREADY_REGISTERED)
            {
                errorMessage = $"The shortcut '{gesture}' is already in use by another application. Please choose a different shortcut.";
            }
            else
            {
                errorMessage = $"Failed to register shortcut '{gesture}' (Win32 Error code: {errorCode}).";
            }
            return false;
        }

        _currentHotkey = gesture;
        _isRegistered = true;
        return true;
    }

    public void Unregister()
    {
        if (_isRegistered && _messageWindow != null && _messageWindow.Handle != IntPtr.Zero)
        {
            NativeMethods.UnregisterHotKey(_messageWindow.Handle, HotkeyId);
        }

        _isRegistered = false;
        _currentHotkey = null;
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == NativeMethods.WM_HOTKEY && wParam.ToInt32() == HotkeyId)
        {
            HotkeyPressed?.Invoke(this, EventArgs.Empty);
            handled = true;
        }

        return IntPtr.Zero;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        Unregister();

        if (_messageWindow != null)
        {
            _messageWindow.Dispose();
            _messageWindow = null;
        }
    }
}
