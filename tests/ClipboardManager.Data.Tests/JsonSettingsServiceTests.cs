using ClipboardManager.Core.Models;
using ClipboardManager.Data.Services;
using Xunit;

namespace ClipboardManager.Data.Tests;

public class JsonSettingsServiceTests : IDisposable
{
    private readonly string _testSettingsPath;

    public JsonSettingsServiceTests()
    {
        _testSettingsPath = Path.Combine(Path.GetTempPath(), $"ClipboardManager_TestSettings_{Guid.NewGuid():N}.json");
    }

    public void Dispose()
    {
        if (File.Exists(_testSettingsPath))
        {
            try
            {
                File.Delete(_testSettingsPath);
            }
            catch
            {
                // ignore test cleanup error
            }
        }
    }

    [Fact]
    public async Task LoadSettingsAsync_WhenFileDoesNotExist_ReturnsDefaultSettings()
    {
        // Arrange
        var service = new JsonSettingsService(_testSettingsPath);

        // Act
        var settings = await service.LoadSettingsAsync();

        // Assert
        Assert.NotNull(settings);
        Assert.Equal(AppSettings.DefaultMaxHistoryItems, settings.MaxHistoryItems);
        Assert.False(settings.IsMonitoringPaused);
        Assert.Equal(HotkeyGesture.Default, settings.Hotkey);
    }

    [Fact]
    public async Task LoadSettingsAsync_WhenFileIsCorruptJson_GracefullyFallsBackToDefaults()
    {
        // Arrange
        await File.WriteAllTextAsync(_testSettingsPath, "{ invalid json @@##$$ missing braces");
        var service = new JsonSettingsService(_testSettingsPath);

        // Act
        var settings = await service.LoadSettingsAsync();

        // Assert: should NOT throw, must return defaults
        Assert.NotNull(settings);
        Assert.Equal(AppSettings.DefaultMaxHistoryItems, settings.MaxHistoryItems);
        Assert.False(settings.IsMonitoringPaused);
    }

    [Fact]
    public async Task SaveSettingsAsync_PersistsAndLoadsCorrectly()
    {
        // Arrange
        var service = new JsonSettingsService(_testSettingsPath);
        var customHotkey = new HotkeyGesture(HotkeyModifiers.Alt | HotkeyModifiers.Control, 0x43, "C");
        var settingsToSave = new AppSettings
        {
            MaxHistoryItems = 500,
            IsMonitoringPaused = true,
            Hotkey = customHotkey
        };

        // Act
        await service.SaveSettingsAsync(settingsToSave);

        var loadedService = new JsonSettingsService(_testSettingsPath);
        var loadedSettings = await loadedService.LoadSettingsAsync();

        // Assert
        Assert.Equal(500, loadedSettings.MaxHistoryItems);
        Assert.True(loadedSettings.IsMonitoringPaused);
        Assert.Equal(customHotkey, loadedSettings.Hotkey);
    }

    [Fact]
    public async Task SaveSettingsAsync_FiresSettingsChangedEvent()
    {
        // Arrange
        var service = new JsonSettingsService(_testSettingsPath);
        bool eventFired = false;
        AppSettings? notifiedSettings = null;

        service.SettingsChanged += (_, s) =>
        {
            eventFired = true;
            notifiedSettings = s;
        };

        var settings = new AppSettings { MaxHistoryItems = 350 };

        // Act
        await service.SaveSettingsAsync(settings);

        // Assert
        Assert.True(eventFired);
        Assert.NotNull(notifiedSettings);
        Assert.Equal(350, notifiedSettings!.MaxHistoryItems);
    }
}
