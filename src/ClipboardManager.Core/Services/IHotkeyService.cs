using ClipboardManager.Core.Models;

namespace ClipboardManager.Core.Services;

/// <summary>
/// Service managing system-wide global hotkeys using Win32 RegisterHotKey.
/// </summary>
public interface IHotkeyService : IDisposable
{
    /// <summary>
    /// Raised when the registered hotkey combination is pressed by the user.
    /// </summary>
    event EventHandler? HotkeyPressed;

    /// <summary>
    /// Currently registered hotkey gesture, or null if registration failed/none set.
    /// </summary>
    HotkeyGesture? CurrentHotkey { get; }

    /// <summary>
    /// True if the hotkey is currently registered with the Windows operating system.
    /// </summary>
    bool IsRegistered { get; }

    /// <summary>
    /// Registers the specified hotkey. If registration fails (e.g. key taken by another app),
    /// returns false with an informative error message.
    /// </summary>
    bool Register(HotkeyGesture gesture, out string? errorMessage);

    /// <summary>
    /// Unregisters the current hotkey from the operating system.
    /// </summary>
    void Unregister();
}
