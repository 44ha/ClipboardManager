using ClipboardManager.Core.Models;
using ClipboardManager.Data.Repositories;
using Xunit;

namespace ClipboardManager.Data.Tests;

public class SqliteClipboardHistoryRepositoryTests : IDisposable
{
    private readonly TestDbContextFactory _factory;
    private readonly SqliteClipboardHistoryRepository _repository;

    public SqliteClipboardHistoryRepositoryTests()
    {
        _factory = new TestDbContextFactory();
        _repository = new SqliteClipboardHistoryRepository(_factory);
    }

    public void Dispose()
    {
        _factory.Dispose();
    }

    [Fact]
    public async Task AddOrUpdateAsync_DuplicateItem_MovesToTopInsteadOfDuplicating()
    {
        // Arrange
        var item1 = ClipboardItem.CreateText("First item");
        var item2 = ClipboardItem.CreateText("Second item");

        await _repository.AddOrUpdateAsync(item1, maxItems: 10);
        await Task.Delay(20); // ensure distinct timestamps
        await _repository.AddOrUpdateAsync(item2, maxItems: 10);

        // Verify initial order: item2 should be first, item1 second
        var initialList = await _repository.GetHistoryAsync();
        Assert.Equal(2, initialList.Count);
        Assert.Equal("Second item", initialList[0].TextContent);
        Assert.Equal("First item", initialList[1].TextContent);

        // Act: re-copy "First item"
        await Task.Delay(20);
        var duplicate = ClipboardItem.CreateText("First item");
        var result = await _repository.AddOrUpdateAsync(duplicate, maxItems: 10);

        // Assert: total count remains 2, but "First item" is now at top
        var updatedList = await _repository.GetHistoryAsync();
        Assert.Equal(2, updatedList.Count);
        Assert.Equal("First item", updatedList[0].TextContent);
        Assert.Equal(item1.Id, updatedList[0].Id);
        Assert.Equal("Second item", updatedList[1].TextContent);
    }

    [Fact]
    public async Task AddOrUpdateAsync_ExceedsMaxItems_RemovesOldestNonPinnedFirst()
    {
        // Arrange: Add 5 items with max 3
        const int maxItems = 3;
        for (int i = 1; i <= 5; i++)
        {
            var item = ClipboardItem.CreateText($"Item {i}");
            await _repository.AddOrUpdateAsync(item, maxItems);
            await Task.Delay(15);
        }

        // Act
        var history = await _repository.GetHistoryAsync();

        // Assert: only the 3 newest items (Item 5, 4, 3) should remain
        Assert.Equal(3, history.Count);
        Assert.Equal("Item 5", history[0].TextContent);
        Assert.Equal("Item 4", history[1].TextContent);
        Assert.Equal("Item 3", history[2].TextContent);
    }

    [Fact]
    public async Task AddOrUpdateAsync_ExceedsMaxItems_NeverRemovesPinnedItems()
    {
        // Arrange: Add pinned item, then add more than maxItems unpinned items
        var pinnedItem = ClipboardItem.CreateText("Important Pinned Note", isPinned: true);
        await _repository.AddOrUpdateAsync(pinnedItem, maxItems: 2);
        // Explicitly set pinned in DB to be certain
        await _repository.SetPinnedAsync(pinnedItem.Id, true);

        // Add 4 more non-pinned items with limit of 2
        for (int i = 1; i <= 4; i++)
        {
            await Task.Delay(15);
            var item = ClipboardItem.CreateText($"Temp Item {i}");
            await _repository.AddOrUpdateAsync(item, maxItems: 2);
        }

        // Act
        var history = await _repository.GetHistoryAsync();

        // Assert: Pinned item MUST still exist at the top!
        // Plus at most 2 unpinned items (the newest ones: Temp Item 4 and Temp Item 3)
        Assert.Contains(history, i => i.TextContent == "Important Pinned Note" && i.IsPinned);
        Assert.True(history[0].IsPinned);
        Assert.Equal("Important Pinned Note", history[0].TextContent);

        var unpinned = history.Where(i => !i.IsPinned).ToList();
        Assert.Equal(2, unpinned.Count);
        Assert.Equal("Temp Item 4", unpinned[0].TextContent);
        Assert.Equal("Temp Item 3", unpinned[1].TextContent);
    }

    [Fact]
    public async Task GetHistoryAsync_SearchQuery_FiltersMatchingText()
    {
        // Arrange
        await _repository.AddOrUpdateAsync(ClipboardItem.CreateText("Password123"), 100);
        await _repository.AddOrUpdateAsync(ClipboardItem.CreateText("Meeting notes at 2pm"), 100);
        await _repository.AddOrUpdateAsync(ClipboardItem.CreateText("Project meeting agenda"), 100);
        await _repository.AddOrUpdateAsync(ClipboardItem.CreateImage(new byte[] { 1, 2, 3 }), 100);

        // Act
        var results = await _repository.GetHistoryAsync("meeting");

        // Assert
        Assert.Equal(2, results.Count);
        Assert.All(results, item => Assert.Contains("meeting", item.TextContent, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task ClearNonPinnedAsync_RemovesOnlyUnpinnedItems_KeepsPinned()
    {
        // Arrange
        var pinned1 = ClipboardItem.CreateText("Pinned 1", isPinned: true);
        var unpinned1 = ClipboardItem.CreateText("Unpinned 1", isPinned: false);
        var unpinned2 = ClipboardItem.CreateText("Unpinned 2", isPinned: false);

        await _repository.AddOrUpdateAsync(pinned1, 100);
        await _repository.SetPinnedAsync(pinned1.Id, true);
        await _repository.AddOrUpdateAsync(unpinned1, 100);
        await _repository.AddOrUpdateAsync(unpinned2, 100);

        // Act
        await _repository.ClearNonPinnedAsync();

        // Assert
        var history = await _repository.GetHistoryAsync();
        Assert.Single(history);
        Assert.Equal("Pinned 1", history[0].TextContent);
        Assert.True(history[0].IsPinned);
    }

    [Fact]
    public async Task DeleteAsync_RemovesSpecificItem()
    {
        // Arrange
        var item1 = ClipboardItem.CreateText("Item to keep");
        var item2 = ClipboardItem.CreateText("Item to delete");

        await _repository.AddOrUpdateAsync(item1, 100);
        await _repository.AddOrUpdateAsync(item2, 100);

        // Act
        await _repository.DeleteAsync(item2.Id);

        // Assert
        var history = await _repository.GetHistoryAsync();
        Assert.Single(history);
        Assert.Equal("Item to keep", history[0].TextContent);
    }
}
