namespace ClipboardManager.Core.Models;

/// <summary>
/// User-configurable application preferences persisted to JSON in %AppData%\ClipboardManager\settings.json.
/// </summary>
public class AppSettings
{
    public const int DefaultMaxHistoryItems = 200;
    public const int MinHistoryItems = 10;
    public const int MaxAllowedHistoryItems = 10000;

    /// <summary>
    /// Global hotkey used to summon the history window.
    /// </summary>
    public HotkeyGesture Hotkey { get; set; } = HotkeyGesture.Default;

    /// <summary>
    /// Maximum number of non-pinned items preserved in SQLite database.
    /// </summary>
    public int MaxHistoryItems { get; set; } = DefaultMaxHistoryItems;

    /// <summary>
    /// Indicates whether clipboard capture is temporarily paused by the user.
    /// </summary>
    public bool IsMonitoringPaused { get; set; } = false;

    /// <summary>
    /// Clones the settings instance for safe editing in settings dialogs.
    /// </summary>
    public AppSettings Clone() => new()
    {
        Hotkey = Hotkey,
        MaxHistoryItems = MaxHistoryItems,
        IsMonitoringPaused = IsMonitoringPaused
    };
}
