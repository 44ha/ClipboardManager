using System.Security.Cryptography;
using System.Text;

namespace ClipboardManager.Core.Common;

/// <summary>
/// Provides cryptographic SHA-256 hashing to reliably detect duplicate clipboard content.
/// </summary>
public static class ContentHasher
{
    public static string ComputeHash(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        byte[] bytes = Encoding.UTF8.GetBytes(text);
        return ComputeHash(bytes);
    }

    public static string ComputeHash(ReadOnlySpan<byte> bytes)
    {
        byte[] hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash);
    }
}
