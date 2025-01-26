using System.Buffers;
using System.Security.Cryptography;
using System.Text;

namespace GLV.Shared.Common;

public static class SHA256Extensions
{
    #region To String

    public static string ToSHA256(this byte[] bytes)
        => ToSHA256(bytes.AsSpan());

    public static string ToSHA256(this Span<byte> bytes)
        => ToSHA256(bytes);

    public static string ToSHA256(this ReadOnlySpan<byte> bytes)
    {
        Span<byte> hash = stackalloc byte[SHA256.HashSizeInBytes];
        SHA256.HashData(bytes, hash);
        return hash.ToHexViaLookup32();
    }

    public static string ToSHA256(this string str)
        => ToSHA256(str.AsSpan());

    public static string ToSHA256(this Span<char> str)
        => ToSHA256((ReadOnlySpan<char>)str);

    public static string ToSHA256(this ReadOnlySpan<char> str)
    {
        byte[]? rented = null;

        var byteLen = Encoding.UTF8.GetByteCount(str);
        Span<byte> bytes = byteLen > 4098 ? (rented = ArrayPool<byte>.Shared.Rent(byteLen)).AsSpan(0, byteLen) : stackalloc byte[byteLen];
        Span<byte> hash = stackalloc byte[SHA256.HashSizeInBytes];
        Encoding.UTF8.GetBytes(str, bytes);

        try
        {
            SHA256.HashData(bytes, hash);
        }
        finally
        {
            if (rented != null)
                ArrayPool<byte>.Shared.Return(rented);
        }

        return hash.ToHexViaLookup32();
    }

    #endregion

    #region To Array

    public static byte[] ToSHA256Array(this byte[] bytes)
        => ToSHA256Array(bytes.AsSpan());

    public static byte[] ToSHA256Array(this Span<byte> bytes)
        => ToSHA256Array(bytes);

    public static byte[] ToSHA256Array(this ReadOnlySpan<byte> bytes)
    {
        Span<byte> hash = stackalloc byte[SHA256.HashSizeInBytes];
        SHA256.HashData(bytes, hash);
        return hash.ToArray();
    }

    public static byte[] ToSHA256Array(this string str)
        => ToSHA256Array(str.AsSpan());

    public static byte[] ToSHA256Array(this Span<char> str)
        => ToSHA256Array((ReadOnlySpan<char>)str);

    public static byte[] ToSHA256Array(this ReadOnlySpan<char> str)
    {
        byte[]? rented = null;

        var byteLen = Encoding.UTF8.GetByteCount(str);
        Span<byte> bytes = byteLen > 4098 ? (rented = ArrayPool<byte>.Shared.Rent(byteLen)).AsSpan(0, byteLen) : stackalloc byte[byteLen];
        Span<byte> hash = stackalloc byte[SHA256.HashSizeInBytes];
        Encoding.UTF8.GetBytes(str, bytes);

        try
        {
            SHA256.HashData(bytes, hash);
        }
        finally
        {
            if (rented != null)
                ArrayPool<byte>.Shared.Return(rented);
        }

        return hash.ToArray();
    }

    #endregion

    #region ToSpan

    public static int TryHashToSHA256(this byte[] bytes, Span<byte> output, bool allowTruncation = false)
        => TryHashToSHA256(bytes.AsSpan(), output, allowTruncation);

    public static int TryHashToSHA256(this Span<byte> bytes, Span<byte> output, bool allowTruncation = false)
        => TryHashToSHA256((ReadOnlySpan<byte>)bytes, output, allowTruncation);

    public static int TryHashToSHA256(this ReadOnlySpan<byte> bytes, Span<byte> output, bool allowTruncation = false)
    {
        if (SHA256.HashSizeInBytes > output.Length && allowTruncation)
        {
            Span<byte> hash = stackalloc byte[SHA256.HashSizeInBytes];
            int written = SHA256.HashData(bytes, hash);
            for (int i = 0; i < output.Length; i++) output[i] = hash[i];
            return hash.Length;
        }

        return SHA256.HashData(bytes, output);
    }

    public static int TryHashToSHA256(this string str, Span<byte> output, bool allowTruncation = false)
        => TryHashToSHA256(str.AsSpan(), output, allowTruncation);

    public static int TryHashToSHA256(this Span<char> str, Span<byte> output, bool allowTruncation = false)
        => TryHashToSHA256((ReadOnlySpan<char>)str, output, allowTruncation);

    public static int TryHashToSHA256(this ReadOnlySpan<char> str, Span<byte> output, bool allowTruncation = false)
    {
        byte[]? rented = null;

        var byteLen = Encoding.UTF8.GetByteCount(str);
        Span<byte> bytes = byteLen > 4098 ? (rented = ArrayPool<byte>.Shared.Rent(byteLen)).AsSpan(0, byteLen) : stackalloc byte[byteLen];
        Encoding.UTF8.GetBytes(str, bytes);

        try
        {
            return TryHashToSHA256(bytes, output, allowTruncation);
        }
        finally
        {
            if (rented != null)
                ArrayPool<byte>.Shared.Return(rented);
        }
    }

    #endregion
}
