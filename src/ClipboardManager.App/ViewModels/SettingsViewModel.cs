using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ClipboardManager.App.Services;
using ClipboardManager.Core.Models;
using ClipboardManager.Core.Services;

namespace ClipboardManager.App.ViewModels;

/// <summary>
/// ViewModel managing user preferences (global hotkey, history retention, monitoring pause, database purge).
/// </summary>
public partial class SettingsViewModel : ObservableObject
{
    private readonly ISettingsService _settingsService;
    private readonly IHotkeyService _hotkeyService;
    private readonly IClipboardHistoryRepository _repository;
    private readonly IClipboardMonitor _clipboardMonitor;
    private readonly IWindowService _windowService;

    [ObservableProperty]
    private string _hotkeyText = "Ctrl + Shift + V";

    [ObservableProperty]
    private int _maxHistoryItems = 200;

    [ObservableProperty]
    private bool _isMonitoringPaused;

    [ObservableProperty]
    private string? _hotkeyErrorMessage;

    [ObservableProperty]
    private string? _statusMessage;

    public SettingsViewModel(
        ISettingsService settingsService,
        IHotkeyService hotkeyService,
        IClipboardHistoryRepository repository,
        IClipboardMonitor clipboardMonitor,
        IWindowService windowService)
    {
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        _hotkeyService = hotkeyService ?? throw new ArgumentNullException(nameof(hotkeyService));
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _clipboardMonitor = clipboardMonitor ?? throw new ArgumentNullException(nameof(clipboardMonitor));
        _windowService = windowService ?? throw new ArgumentNullException(nameof(windowService));

        LoadCurrentSettings();
    }

    public void LoadCurrentSettings()
    {
        var settings = _settingsService.CurrentSettings;
        HotkeyText = settings.Hotkey.ToString();
        MaxHistoryItems = settings.MaxHistoryItems;
        IsMonitoringPaused = settings.IsMonitoringPaused;
        HotkeyErrorMessage = null;
        StatusMessage = null;
    }

    [RelayCommand]
    public async Task SaveAsync()
    {
        HotkeyErrorMessage = null;
        StatusMessage = null;

        // 1. Validate hotkey syntax
        if (!HotkeyGesture.TryParse(HotkeyText, out var parsedGesture) || parsedGesture == null)
        {
            HotkeyErrorMessage = "Invalid hotkey format. Example: Ctrl + Shift + V or Alt + C";
            return;
        }

        // 2. Validate hotkey with operating system
        if (!_hotkeyService.Register(parsedGesture, out string? registrationError))
        {
            HotkeyErrorMessage = registrationError;
            return;
        }

        // 3. Update monitoring pause state
        if (IsMonitoringPaused)
        {
            _clipboardMonitor.Pause();
        }
        else
        {
            _clipboardMonitor.Resume();
        }

        // 4. Persist updated settings to JSON file
        var newSettings = new AppSettings
        {
            Hotkey = parsedGesture,
            MaxHistoryItems = Math.Clamp(MaxHistoryItems, AppSettings.MinHistoryItems, AppSettings.MaxAllowedHistoryItems),
            IsMonitoringPaused = IsMonitoringPaused
        };

        await _settingsService.SaveSettingsAsync(newSettings);

        StatusMessage = "Settings saved successfully!";
        _windowService.HideSettingsWindow();
    }

    [RelayCommand]
    public void ResetDefaults()
    {
        var defaults = new AppSettings();
        HotkeyText = defaults.Hotkey.ToString();
        MaxHistoryItems = defaults.MaxHistoryItems;
        IsMonitoringPaused = defaults.IsMonitoringPaused;
        HotkeyErrorMessage = null;
        StatusMessage = "Reset to defaults. Click Save to apply.";
    }

    [RelayCommand]
    public async Task ClearAllHistoryAsync()
    {
        await _repository.ClearAllAsync();
        StatusMessage = "Clipboard history completely cleared.";
    }

    [RelayCommand]
    public void Close()
    {
        _windowService.HideSettingsWindow();
    }
}
