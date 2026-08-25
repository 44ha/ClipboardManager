using ClipboardManager.Core.Models;
using Xunit;

namespace ClipboardManager.Core.Tests;

public class ClipboardItemTests
{
    [Fact]
    public void CreateText_InitializesPropertiesCorrectly()
    {
        // Arrange
        const string text = "Sample copied text";

        // Act
        var item = ClipboardItem.CreateText(text);

        // Assert
        Assert.NotEqual(Guid.Empty, item.Id);
        Assert.Equal(ClipboardContentType.Text, item.ContentType);
        Assert.Equal(text, item.TextContent);
        Assert.Null(item.ImageData);
        Assert.False(string.IsNullOrWhiteSpace(item.ContentHash));
        Assert.False(item.IsPinned);
        Assert.True(item.CopiedAt <= DateTime.UtcNow);
    }

    [Fact]
    public void CreateText_EmptyOrWhitespace_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => ClipboardItem.CreateText(""));
        Assert.Throws<ArgumentException>(() => ClipboardItem.CreateText("   "));
    }

    [Fact]
    public void CreateImage_InitializesPropertiesCorrectly()
    {
        // Arrange
        byte[] imageData = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };

        // Act
        var item = ClipboardItem.CreateImage(imageData, isPinned: true);

        // Assert
        Assert.NotEqual(Guid.Empty, item.Id);
        Assert.Equal(ClipboardContentType.Image, item.ContentType);
        Assert.Null(item.TextContent);
        Assert.Equal(imageData, item.ImageData);
        Assert.False(string.IsNullOrWhiteSpace(item.ContentHash));
        Assert.True(item.IsPinned);
    }

    [Fact]
    public void CreateImage_EmptyBytes_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => ClipboardItem.CreateImage(Array.Empty<byte>()));
    }

    [Fact]
    public void PreviewText_ForText_CollapsesWhitespaceAndTruncates()
    {
        // Arrange
        string longText = "First line of clipboard content\r\nSecond line has more details that make this string longer than eighty characters in total.";
        var item = ClipboardItem.CreateText(longText);

        // Act
        string preview = item.PreviewText;

        // Assert
        Assert.DoesNotContain("\r\n", preview);
        Assert.EndsWith("…", preview);
        Assert.True(preview.Length <= 85);
    }

    [Fact]
    public void PreviewText_ForImage_DisplaysImageBadgeWithKilobytes()
    {
        // Arrange: 2 KB dummy buffer
        byte[] buffer = new byte[2048];
        var item = ClipboardItem.CreateImage(buffer);

        // Act
        string preview = item.PreviewText;

        // Assert
        Assert.Equal("[Image • 2 KB]", preview);
    }
}
