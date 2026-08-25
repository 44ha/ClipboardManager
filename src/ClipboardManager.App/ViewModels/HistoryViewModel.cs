using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ClipboardManager.App.Services;
using ClipboardManager.Core.Models;
using ClipboardManager.Core.Services;

namespace ClipboardManager.App.ViewModels;

/// <summary>
/// ViewModel managing clipboard history presentation, searching, pinning, and item restoration.
/// Contains no Win32 or EF Core calls; coordinates strictly through injected service interfaces.
/// </summary>
public partial class HistoryViewModel : ObservableObject
{
    private readonly IClipboardHistoryRepository _repository;
    private readonly IClipboardMonitor _clipboardMonitor;
    private readonly IClipboardPaster _clipboardPaster;
    private readonly ISettingsService _settingsService;
    private readonly IWindowService _windowService;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private ClipboardItemViewModel? _selectedItem;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _isEmpty;

    [ObservableProperty]
    private int _totalCount;

    [ObservableProperty]
    private int _pinnedCount;

    public ObservableCollection<ClipboardItemViewModel> Items { get; } = new();

    public HistoryViewModel(
        IClipboardHistoryRepository repository,
        IClipboardMonitor clipboardMonitor,
        IClipboardPaster clipboardPaster,
        ISettingsService settingsService,
        IWindowService windowService)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _clipboardMonitor = clipboardMonitor ?? throw new ArgumentNullException(nameof(clipboardMonitor));
        _clipboardPaster = clipboardPaster ?? throw new ArgumentNullException(nameof(clipboardPaster));
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        _windowService = windowService ?? throw new ArgumentNullException(nameof(windowService));

        // When a new item is captured in the background, refresh the list
        _clipboardMonitor.ClipboardContentCaptured += OnClipboardContentCaptured;
    }

    private void OnClipboardContentCaptured(object? sender, ClipboardItem e)
    {
        // Safe dispatch to UI thread
        Application.Current?.Dispatcher.InvokeAsync(async () =>
        {
            // Store item in repository
            int maxItems = _settingsService.CurrentSettings.MaxHistoryItems;
            await _repository.AddOrUpdateAsync(e, maxItems);

            // Refresh UI list if search is empty or matches
            await LoadHistoryAsync();
        });
    }

    partial void OnSearchTextChanged(string value)
    {
        _ = LoadHistoryAsync();
    }

    [RelayCommand]
    public async Task LoadHistoryAsync()
    {
        IsLoading = true;
        try
        {
            var rawItems = await _repository.GetHistoryAsync(SearchText);

            Items.Clear();
            foreach (var item in rawItems)
            {
                Items.Add(new ClipboardItemViewModel(item));
            }

            IsEmpty = Items.Count == 0;
            if (Items.Count > 0 && SelectedItem == null)
            {
                SelectedItem = Items[0];
            }

            var (total, pinned, _) = await _repository.GetCountsAsync();
            TotalCount = total;
            PinnedCount = pinned;
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task SelectAndPasteAsync(ClipboardItemViewModel? item)
    {
        var target = item ?? SelectedItem;
        if (target == null)
        {
            return;
        }

        // Put item back onto the active clipboard
        await _clipboardPaster.SetClipboardAsync(target.ToModel());

        // Close/hide history window
        _windowService.HideHistoryWindow();
    }

    [RelayCommand]
    public async Task TogglePinAsync(ClipboardItemViewModel? item)
    {
        if (item == null) return;

        bool newPinState = !item.IsPinned;
        await _repository.SetPinnedAsync(item.Id, newPinState);

        // Reload so pinned items stay properly grouped at the top
        await LoadHistoryAsync();
    }

    [RelayCommand]
    public async Task DeleteItemAsync(ClipboardItemViewModel? item)
    {
        if (item == null) return;

        await _repository.DeleteAsync(item.Id);
        Items.Remove(item);

        IsEmpty = Items.Count == 0;
        var (total, pinned, _) = await _repository.GetCountsAsync();
        TotalCount = total;
        PinnedCount = pinned;
    }

    [RelayCommand]
    public async Task ClearNonPinnedAsync()
    {
        await _repository.ClearNonPinnedAsync();
        await LoadHistoryAsync();
    }

    [RelayCommand]
    public void OpenSettings()
    {
        _windowService.ShowSettingsWindow();
    }
}
