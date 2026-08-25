using ClipboardManager.Core.Common;

namespace ClipboardManager.Core.Models;

/// <summary>
/// Domain model representing a single captured clipboard item (text or image).
/// </summary>
public class ClipboardItem
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public ClipboardContentType ContentType { get; init; }

    public string? TextContent { get; init; }

    public byte[]? ImageData { get; init; }

    /// <summary>
    /// Hex-encoded SHA-256 hash of the content, used to detect duplicate copies.
    /// </summary>
    public string ContentHash { get; init; }

    public DateTime CopiedAt { get; set; } = DateTime.UtcNow;

    public bool IsPinned { get; set; }

    /// <summary>
    /// Creates a new text clipboard item with automatically calculated SHA-256 hash.
    /// </summary>
    public static ClipboardItem CreateText(string text, bool isPinned = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);

        return new ClipboardItem
        {
            Id = Guid.NewGuid(),
            ContentType = ClipboardContentType.Text,
            TextContent = text,
            ContentHash = ContentHasher.ComputeHash(text),
            CopiedAt = DateTime.UtcNow,
            IsPinned = isPinned
        };
    }

    /// <summary>
    /// Creates a new image clipboard item with automatically calculated SHA-256 hash.
    /// </summary>
    public static ClipboardItem CreateImage(byte[] imageData, bool isPinned = false)
    {
        ArgumentNullException.ThrowIfNull(imageData);
        if (imageData.Length == 0)
        {
            throw new ArgumentException("Image data cannot be empty.", nameof(imageData));
        }

        return new ClipboardItem
        {
            Id = Guid.NewGuid(),
            ContentType = ClipboardContentType.Image,
            ImageData = imageData,
            ContentHash = ContentHasher.ComputeHash(imageData),
            CopiedAt = DateTime.UtcNow,
            IsPinned = isPinned
        };
    }

    /// <summary>
    /// Concise single-line preview string for UI listing.
    /// </summary>
    public string PreviewText
    {
        get
        {
            if (ContentType == ClipboardContentType.Image)
            {
                int kb = (ImageData?.Length ?? 0) / 1024;
                return $"[Image • {kb} KB]";
            }

            if (string.IsNullOrWhiteSpace(TextContent))
            {
                return string.Empty;
            }

            // Collapse multiple spaces/newlines into a single preview line
            string singleLine = TextContent.Replace("\r\n", " ").Replace('\n', ' ').Replace('\r', ' ').Trim();
            return singleLine.Length > 80 ? string.Concat(singleLine.AsSpan(0, 80), "…") : singleLine;
        }
    }

    // Required for ORM / deserialization
    public ClipboardItem()
    {
        ContentHash = string.Empty;
    }
}
