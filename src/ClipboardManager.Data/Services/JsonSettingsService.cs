using System.Text.Json;
using ClipboardManager.Core.Models;
using ClipboardManager.Core.Services;

namespace ClipboardManager.Data.Services;

/// <summary>
/// Persists AppSettings in JSON format.
/// Gracefully handles missing files, empty files, or corrupt syntax by falling back to safe defaults.
/// </summary>
public class JsonSettingsService : ISettingsService
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    private readonly string _filePath;
    private readonly SemaphoreSlim _fileLock = new(1, 1);
    private AppSettings _currentSettings = new();

    public AppSettings CurrentSettings => _currentSettings;

    public event EventHandler<AppSettings>? SettingsChanged;

    /// <summary>
    /// Default constructor uses standard %AppData%\ClipboardManager\settings.json.
    /// </summary>
    public JsonSettingsService()
        : this(GetDefaultSettingsFilePath())
    {
    }

    /// <summary>
    /// Overload allowing custom path, essential for isolated unit tests.
    /// </summary>
    public JsonSettingsService(string filePath)
    {
        _filePath = !string.IsNullOrWhiteSpace(filePath)
            ? filePath
            : GetDefaultSettingsFilePath();
    }

    public static string GetDefaultSettingsFilePath()
    {
        string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(appData, "ClipboardManager", "settings.json");
    }

    public async Task<AppSettings> LoadSettingsAsync(CancellationToken cancellationToken = default)
    {
        await _fileLock.WaitAsync(cancellationToken);
        try
        {
            if (!File.Exists(_filePath))
            {
                _currentSettings = new AppSettings();
                return _currentSettings;
            }

            try
            {
                string json = await File.ReadAllTextAsync(_filePath, cancellationToken);
                if (string.IsNullOrWhiteSpace(json))
                {
                    _currentSettings = new AppSettings();
                    return _currentSettings;
                }

                var loaded = JsonSerializer.Deserialize<AppSettings>(json, SerializerOptions);
                _currentSettings = loaded ?? new AppSettings();
            }
            catch (Exception)
            {
                // Fall back safely to defaults on corrupt JSON or read errors
                _currentSettings = new AppSettings();
            }

            return _currentSettings;
        }
        finally
        {
            _fileLock.Release();
        }
    }

    public async Task SaveSettingsAsync(AppSettings settings, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(settings);

        await _fileLock.WaitAsync(cancellationToken);
        try
        {
            string directory = Path.GetDirectoryName(_filePath)!;
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            string json = JsonSerializer.Serialize(settings, SerializerOptions);
            await File.WriteAllTextAsync(_filePath, json, cancellationToken);

            _currentSettings = settings.Clone();
        }
        finally
        {
            _fileLock.Release();
        }

        SettingsChanged?.Invoke(this, _currentSettings);
    }
}
