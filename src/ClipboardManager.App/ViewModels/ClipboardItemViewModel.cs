using CommunityToolkit.Mvvm.ComponentModel;
using ClipboardManager.Core.Models;

namespace ClipboardManager.App.ViewModels;

/// <summary>
/// ViewModel representing a single history row in the list view.
/// </summary>
public partial class ClipboardItemViewModel : ObservableObject
{
    public Guid Id { get; }

    public ClipboardContentType ContentType { get; }

    public string? TextContent { get; }

    public byte[]? ImageData { get; }

    public string ContentHash { get; }

    public DateTime CopiedAt { get; }

    public string PreviewText { get; }

    public bool IsImage => ContentType == ClipboardContentType.Image;

    public bool IsText => ContentType == ClipboardContentType.Text;

    [ObservableProperty]
    private bool _isPinned;

    public ClipboardItemViewModel(ClipboardItem model)
    {
        ArgumentNullException.ThrowIfNull(model);

        Id = model.Id;
        ContentType = model.ContentType;
        TextContent = model.TextContent;
        ImageData = model.ImageData;
        ContentHash = model.ContentHash;
        CopiedAt = model.CopiedAt;
        _isPinned = model.IsPinned;
        PreviewText = model.PreviewText;
    }

    public ClipboardItem ToModel() => new()
    {
        Id = Id,
        ContentType = ContentType,
        TextContent = TextContent,
        ImageData = ImageData,
        ContentHash = ContentHash,
        CopiedAt = CopiedAt,
        IsPinned = IsPinned
    };
}
