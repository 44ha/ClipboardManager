namespace ClipboardManager.App.Services;

/// <summary>
/// Abstraction for presenting and toggling desktop windows from ViewModels without tight coupling to UI windows.
/// </summary>
public interface IWindowService
{
    void ShowHistoryWindow();
    void HideHistoryWindow();
    void ToggleHistoryWindow();
    void ShowSettingsWindow();
    void HideSettingsWindow();
}
