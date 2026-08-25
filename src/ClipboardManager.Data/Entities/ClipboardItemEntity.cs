using System.ComponentModel.DataAnnotations;
using ClipboardManager.Core.Models;

namespace ClipboardManager.Data.Entities;

/// <summary>
/// Database entity for clipboard items stored in SQLite.
/// </summary>
public class ClipboardItemEntity
{
    [Key]
    public Guid Id { get; set; }

    public int ContentType { get; set; }

    public string? TextContent { get; set; }

    public byte[]? ImageData { get; set; }

    [Required]
    [MaxLength(64)]
    public string ContentHash { get; set; } = string.Empty;

    public DateTime CopiedAt { get; set; }

    public bool IsPinned { get; set; }

    /// <summary>
    /// Maps from database entity to pure domain model.
    /// </summary>
    public ClipboardItem ToDomain() => new()
    {
        Id = Id,
        ContentType = (ClipboardContentType)ContentType,
        TextContent = TextContent,
        ImageData = ImageData,
        ContentHash = ContentHash,
        CopiedAt = CopiedAt,
        IsPinned = IsPinned
    };

    /// <summary>
    /// Creates a database entity from a domain model.
    /// </summary>
    public static ClipboardItemEntity FromDomain(ClipboardItem domain) => new()
    {
        Id = domain.Id,
        ContentType = (int)domain.ContentType,
        TextContent = domain.TextContent,
        ImageData = domain.ImageData,
        ContentHash = domain.ContentHash,
        CopiedAt = domain.CopiedAt,
        IsPinned = domain.IsPinned
    };
}
