using ClipboardManager.Core.Models;
using Xunit;

namespace ClipboardManager.Core.Tests;

public class HotkeyGestureTests
{
    [Fact]
    public void Default_ReturnsCtrlShiftV()
    {
        var gesture = HotkeyGesture.Default;

        Assert.Equal(HotkeyModifiers.Control | HotkeyModifiers.Shift, gesture.Modifiers);
        Assert.Equal(0x56, gesture.VirtualKey);
        Assert.Equal("V", gesture.KeyName);
        Assert.Equal("Ctrl + Shift + V", gesture.ToString());
    }

    [Theory]
    [InlineData("Ctrl + Shift + V", HotkeyModifiers.Control | HotkeyModifiers.Shift, 0x56, "V")]
    [InlineData("ctrl+shift+v", HotkeyModifiers.Control | HotkeyModifiers.Shift, 0x56, "V")]
    [InlineData("Alt + C", HotkeyModifiers.Alt, 0x43, "C")]
    [InlineData("Win + F1", HotkeyModifiers.Windows, 0x70, "F1")]
    [InlineData("Ctrl + Alt + Insert", HotkeyModifiers.Control | HotkeyModifiers.Alt, 0x2D, "INSERT")]
    public void TryParse_ValidInput_ParsesCorrectly(string input, HotkeyModifiers expectedModifiers, int expectedVk, string expectedKey)
    {
        bool success = HotkeyGesture.TryParse(input, out var gesture);

        Assert.True(success);
        Assert.NotNull(gesture);
        Assert.Equal(expectedModifiers, gesture.Modifiers);
        Assert.Equal(expectedVk, gesture.VirtualKey);
        Assert.Equal(expectedKey, gesture.KeyName);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("Ctrl + ")]
    [InlineData("UnknownModifier + V")]
    public void TryParse_InvalidInput_ReturnsFalse(string? input)
    {
        bool success = HotkeyGesture.TryParse(input, out var gesture);

        Assert.False(success);
        Assert.Null(gesture);
    }
}
