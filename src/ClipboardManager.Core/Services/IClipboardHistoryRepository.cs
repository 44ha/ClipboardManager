using ClipboardManager.Core.Models;

namespace ClipboardManager.Core.Services;

/// <summary>
/// Contract for managing clipboard history persistence.
/// </summary>
public interface IClipboardHistoryRepository
{
    /// <summary>
    /// Retrieves all clipboard items, ordered with pinned items first, then by CopiedAt descending.
    /// Optionally filters items by search query matching text contents.
    /// </summary>
    Task<IReadOnlyList<ClipboardItem>> GetHistoryAsync(string? searchQuery = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a single clipboard item by its identifier.
    /// </summary>
    Task<ClipboardItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new clipboard item or, if an identical item already exists (matching ContentHash),
    /// updates its timestamp to move it to the top. Enforces the max non-pinned capacity.
    /// </summary>
    Task<ClipboardItem> AddOrUpdateAsync(ClipboardItem item, int maxItems, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the pinned state of an existing item.
    /// </summary>
    Task SetPinnedAsync(Guid id, bool isPinned, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a specific item by its identifier.
    /// </summary>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes all items except those that are pinned.
    /// </summary>
    Task ClearNonPinnedAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes all history items, including pinned ones.
    /// </summary>
    Task ClearAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current count of stored items (total, pinned, non-pinned).
    /// </summary>
    Task<(int Total, int Pinned, int Unpinned)> GetCountsAsync(CancellationToken cancellationToken = default);
}
