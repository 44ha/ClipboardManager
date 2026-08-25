using ClipboardManager.Core.Models;

namespace ClipboardManager.Core.Services;

/// <summary>
/// Service responsible for setting items back onto the active system clipboard.
/// </summary>
public interface IClipboardPaster
{
    /// <summary>
    /// Places the selected clipboard item back into the system clipboard.
    /// </summary>
    Task SetClipboardAsync(ClipboardItem item, CancellationToken cancellationToken = default);
}
