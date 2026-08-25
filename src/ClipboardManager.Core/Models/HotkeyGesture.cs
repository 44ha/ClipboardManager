using System.Text;

namespace ClipboardManager.Core.Models;

/// <summary>
/// Immutable representation of a global keyboard shortcut (modifiers + key).
/// Independent of WPF so it can live in the Core domain layer.
/// </summary>
public sealed record HotkeyGesture
{
    public HotkeyModifiers Modifiers { get; init; }

    /// <summary>
    /// Standard Win32 Virtual Key code (e.g. 0x56 for 'V').
    /// </summary>
    public int VirtualKey { get; init; }

    /// <summary>
    /// Human-readable key name (e.g. "V", "F1", "Insert").
    /// </summary>
    public string KeyName { get; init; }

    public HotkeyGesture(HotkeyModifiers modifiers, int virtualKey, string keyName)
    {
        Modifiers = modifiers;
        VirtualKey = virtualKey;
        KeyName = string.IsNullOrWhiteSpace(keyName) ? $"VK_{virtualKey:X2}" : keyName.ToUpperInvariant();
    }

    /// <summary>
    /// Default global hotkey: Ctrl + Shift + V (Virtual Key 0x56).
    /// </summary>
    public static HotkeyGesture Default => new(
        HotkeyModifiers.Control | HotkeyModifiers.Shift,
        0x56, // 'V'
        "V"
    );

    public override string ToString()
    {
        var sb = new StringBuilder();

        if (Modifiers.HasFlag(HotkeyModifiers.Control))
        {
            sb.Append("Ctrl + ");
        }

        if (Modifiers.HasFlag(HotkeyModifiers.Alt))
        {
            sb.Append("Alt + ");
        }

        if (Modifiers.HasFlag(HotkeyModifiers.Shift))
        {
            sb.Append("Shift + ");
        }

        if (Modifiers.HasFlag(HotkeyModifiers.Windows))
        {
            sb.Append("Win + ");
        }

        sb.Append(KeyName);
        return sb.ToString();
    }

    /// <summary>
    /// Parses a string representation like "Ctrl + Shift + V" or "Alt + V".
    /// </summary>
    public static bool TryParse(string? text, out HotkeyGesture? gesture)
    {
        gesture = null;
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        string[] parts = text.Split(new[] { '+', ' ' }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
        {
            return false;
        }

        var modifiers = HotkeyModifiers.None;
        string? keyPart = null;

        foreach (string part in parts)
        {
            string upper = part.Trim().ToUpperInvariant();
            switch (upper)
            {
                case "CTRL":
                case "CONTROL":
                    modifiers |= HotkeyModifiers.Control;
                    break;
                case "ALT":
                    modifiers |= HotkeyModifiers.Alt;
                    break;
                case "SHIFT":
                    modifiers |= HotkeyModifiers.Shift;
                    break;
                case "WIN":
                case "WINDOWS":
                    modifiers |= HotkeyModifiers.Windows;
                    break;
                default:
                    if (keyPart is not null)
                    {
                        // More than one key component specified (e.g. UnknownModifier + V)
                        return false;
                    }
                    keyPart = upper;
                    break;
            }
        }

        if (string.IsNullOrEmpty(keyPart))
        {
            return false;
        }

        int vk = ResolveVirtualKey(keyPart);
        if (vk == 0)
        {
            return false;
        }

        gesture = new HotkeyGesture(modifiers, vk, keyPart);
        return true;
    }

    private static int ResolveVirtualKey(string keyName)
    {
        if (keyName.Length == 1 && keyName[0] >= 'A' && keyName[0] <= 'Z')
        {
            return keyName[0];
        }

        if (keyName.Length == 1 && keyName[0] >= '0' && keyName[0] <= '9')
        {
            return keyName[0];
        }

        if (keyName.StartsWith("F", StringComparison.OrdinalIgnoreCase) &&
            int.TryParse(keyName[1..], out int fIndex) && fIndex >= 1 && fIndex <= 24)
        {
            return 0x6F + fIndex; // VK_F1 is 0x70
        }

        return keyName switch
        {
            "SPACE" => 0x20,
            "INSERT" or "INS" => 0x2D,
            "DELETE" or "DEL" => 0x2E,
            "HOME" => 0x24,
            "END" => 0x23,
            "PAGEUP" or "PGUP" => 0x21,
            "PAGEDOWN" or "PGDN" => 0x22,
            "TAB" => 0x09,
            "BACKSPACE" or "BACK" => 0x08,
            "ENTER" or "RETURN" => 0x0D,
            "ESCAPE" or "ESC" => 0x1B,
            _ => 0
        };
    }
}
