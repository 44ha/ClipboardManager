using ClipboardManager.Core.Common;
using Xunit;

namespace ClipboardManager.Core.Tests;

public class ContentHasherTests
{
    [Fact]
    public void ComputeHash_String_ProducesDeterministicSha256Hex()
    {
        // Arrange
        const string input1 = "Hello, Clipboard Manager!";
        const string input2 = "Hello, Clipboard Manager!";
        const string different = "Different clipboard string";

        // Act
        string hash1 = ContentHasher.ComputeHash(input1);
        string hash2 = ContentHasher.ComputeHash(input2);
        string hashDifferent = ContentHasher.ComputeHash(different);

        // Assert
        Assert.Equal(64, hash1.Length); // 256 bits = 64 hex chars
        Assert.Equal(hash1, hash2);
        Assert.NotEqual(hash1, hashDifferent);
    }

    [Fact]
    public void ComputeHash_Bytes_ProducesDeterministicHash()
    {
        // Arrange
        byte[] bytes1 = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };
        byte[] bytes2 = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };
        byte[] differentBytes = new byte[] { 8, 7, 6, 5, 4, 3, 2, 1 };

        // Act
        string hash1 = ContentHasher.ComputeHash(bytes1);
        string hash2 = ContentHasher.ComputeHash(bytes2);
        string hashDifferent = ContentHasher.ComputeHash(differentBytes);

        // Assert
        Assert.Equal(hash1, hash2);
        Assert.NotEqual(hash1, hashDifferent);
    }

    [Fact]
    public void ComputeHash_Null_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => ContentHasher.ComputeHash((string)null!));
    }
}
