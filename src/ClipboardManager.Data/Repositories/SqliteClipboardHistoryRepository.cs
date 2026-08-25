using ClipboardManager.Core.Models;
using ClipboardManager.Core.Services;
using ClipboardManager.Data.Context;
using ClipboardManager.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace ClipboardManager.Data.Repositories;

/// <summary>
/// SQLite implementation of IClipboardHistoryRepository using EF Core.
/// Uses IDbContextFactory to ensure thread safety across concurrent background clipboard captures and UI queries.
/// </summary>
public class SqliteClipboardHistoryRepository : IClipboardHistoryRepository
{
    private readonly IDbContextFactory<ClipboardDbContext> _contextFactory;
    private readonly SemaphoreSlim _writeLock = new(1, 1);

    public SqliteClipboardHistoryRepository(IDbContextFactory<ClipboardDbContext> contextFactory)
    {
        _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
    }

    public async Task<IReadOnlyList<ClipboardItem>> GetHistoryAsync(string? searchQuery = null, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        IQueryable<ClipboardItemEntity> query = context.ClipboardItems.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            string trimmed = searchQuery.Trim();
            // Search text items matching substring (case-insensitive in SQLite LIKE)
            query = query.Where(e => e.ContentType == (int)ClipboardContentType.Text &&
                                     e.TextContent != null &&
                                     EF.Functions.Like(e.TextContent, $"%{trimmed}%"));
        }

        // Pinned items stay at top, then ordered newest first
        var entities = await query
            .OrderByDescending(e => e.IsPinned)
            .ThenByDescending(e => e.CopiedAt)
            .ToListAsync(cancellationToken);

        return entities.Select(e => e.ToDomain()).ToList();
    }

    public async Task<ClipboardItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var entity = await context.ClipboardItems.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
        return entity?.ToDomain();
    }

    public async Task<ClipboardItem> AddOrUpdateAsync(ClipboardItem item, int maxItems, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(item);

        await _writeLock.WaitAsync(cancellationToken);
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

            // 1. Check for duplicate content using SHA-256 hash
            var existing = await context.ClipboardItems
                .FirstOrDefaultAsync(e => e.ContentHash == item.ContentHash, cancellationToken);

            if (existing != null)
            {
                // If identical item exists, move it to the top (update CopiedAt)
                existing.CopiedAt = DateTime.UtcNow;
                await context.SaveChangesAsync(cancellationToken);
                return existing.ToDomain();
            }

            // 2. Insert new item
            var newEntity = ClipboardItemEntity.FromDomain(item);
            newEntity.CopiedAt = DateTime.UtcNow;
            context.ClipboardItems.Add(newEntity);
            await context.SaveChangesAsync(cancellationToken);

            // 3. Enforce maximum history capacity on non-pinned items
            if (maxItems > 0)
            {
                int unpinnedCount = await context.ClipboardItems
                    .CountAsync(e => !e.IsPinned, cancellationToken);

                if (unpinnedCount > maxItems)
                {
                    int itemsToRemove = unpinnedCount - maxItems;

                    // Oldest non-pinned items are removed first
                    var oldestUnpinned = await context.ClipboardItems
                        .Where(e => !e.IsPinned)
                        .OrderBy(e => e.CopiedAt)
                        .Take(itemsToRemove)
                        .ToListAsync(cancellationToken);

                    context.ClipboardItems.RemoveRange(oldestUnpinned);
                    await context.SaveChangesAsync(cancellationToken);
                }
            }

            return newEntity.ToDomain();
        }
        finally
        {
            _writeLock.Release();
        }
    }

    public async Task SetPinnedAsync(Guid id, bool isPinned, CancellationToken cancellationToken = default)
    {
        await _writeLock.WaitAsync(cancellationToken);
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
            var entity = await context.ClipboardItems.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
            if (entity != null)
            {
                entity.IsPinned = isPinned;
                await context.SaveChangesAsync(cancellationToken);
            }
        }
        finally
        {
            _writeLock.Release();
        }
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await _writeLock.WaitAsync(cancellationToken);
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
            var entity = await context.ClipboardItems.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
            if (entity != null)
            {
                context.ClipboardItems.Remove(entity);
                await context.SaveChangesAsync(cancellationToken);
            }
        }
        finally
        {
            _writeLock.Release();
        }
    }

    public async Task ClearNonPinnedAsync(CancellationToken cancellationToken = default)
    {
        await _writeLock.WaitAsync(cancellationToken);
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
            var nonPinned = await context.ClipboardItems
                .Where(e => !e.IsPinned)
                .ToListAsync(cancellationToken);

            if (nonPinned.Count > 0)
            {
                context.ClipboardItems.RemoveRange(nonPinned);
                await context.SaveChangesAsync(cancellationToken);
            }
        }
        finally
        {
            _writeLock.Release();
        }
    }

    public async Task ClearAllAsync(CancellationToken cancellationToken = default)
    {
        await _writeLock.WaitAsync(cancellationToken);
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
            var all = await context.ClipboardItems.ToListAsync(cancellationToken);
            if (all.Count > 0)
            {
                context.ClipboardItems.RemoveRange(all);
                await context.SaveChangesAsync(cancellationToken);
            }
        }
        finally
        {
            _writeLock.Release();
        }
    }

    public async Task<(int Total, int Pinned, int Unpinned)> GetCountsAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        int total = await context.ClipboardItems.CountAsync(cancellationToken);
        int pinned = await context.ClipboardItems.CountAsync(e => e.IsPinned, cancellationToken);
        return (total, pinned, total - pinned);
    }
}
