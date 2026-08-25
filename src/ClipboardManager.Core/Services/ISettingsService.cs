using ClipboardManager.Core.Models;

namespace ClipboardManager.Core.Services;

/// <summary>
/// Service managing persistent application settings stored in JSON format.
/// Handles fallback to sensible defaults if the file is missing or corrupted.
/// </summary>
public interface ISettingsService
{
    /// <summary>
    /// Current in-memory settings snapshot.
    /// </summary>
    AppSettings CurrentSettings { get; }

    /// <summary>
    /// Raised whenever settings are updated and saved.
    /// </summary>
    event EventHandler<AppSettings>? SettingsChanged;

    /// <summary>
    /// Loads settings from disk, falling back to defaults on error or missing file.
    /// </summary>
    Task<AppSettings> LoadSettingsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves settings to disk and notifies subscribers.
    /// </summary>
    Task SaveSettingsAsync(AppSettings settings, CancellationToken cancellationToken = default);
}
