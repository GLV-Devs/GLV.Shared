using System.Buffers;
using System.Security.Cryptography;
using System.Text;

namespace GLV.Shared.Common;

public static class SHA512Extensions
{
    #region To String

    public static string ToSHA512(this byte[] bytes)
        => ToSHA512(bytes.AsSpan());

    public static string ToSHA512(this Span<byte> bytes)
        => ToSHA512(bytes);

    public static string ToSHA512(this ReadOnlySpan<byte> bytes)
    {
        Span<byte> hash = stackalloc byte[SHA512.HashSizeInBytes];
        SHA512.HashData(bytes, hash);
        return hash.ToHexViaLookup32();
    }

    public static string ToSHA512(this string str)
        => ToSHA512(str.AsSpan());

    public static string ToSHA512(this Span<char> str)
        => ToSHA512((ReadOnlySpan<char>)str);

    public static string ToSHA512(this ReadOnlySpan<char> str)
    {
        byte[]? rented = null;

        var byteLen = Encoding.UTF8.GetByteCount(str);
        Span<byte> bytes = byteLen > 4098 ? (rented = ArrayPool<byte>.Shared.Rent(byteLen)).AsSpan(0, byteLen) : stackalloc byte[byteLen];
        Span<byte> hash = stackalloc byte[SHA512.HashSizeInBytes];
        Encoding.UTF8.GetBytes(str, bytes);

        try
        {
            SHA512.HashData(bytes, hash);
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

    public static byte[] ToSHA512Array(this byte[] bytes)
        => ToSHA512Array(bytes.AsSpan());

    public static byte[] ToSHA512Array(this Span<byte> bytes)
        => ToSHA512Array(bytes);

    public static byte[] ToSHA512Array(this ReadOnlySpan<byte> bytes)
    {
        Span<byte> hash = stackalloc byte[SHA512.HashSizeInBytes];
        SHA512.HashData(bytes, hash);
        return hash.ToArray();
    }

    public static byte[] ToSHA512Array(this string str)
        => ToSHA512Array(str.AsSpan());

    public static byte[] ToSHA512Array(this Span<char> str)
        => ToSHA512Array((ReadOnlySpan<char>)str);

    public static byte[] ToSHA512Array(this ReadOnlySpan<char> str)
    {
        byte[]? rented = null;

        var byteLen = Encoding.UTF8.GetByteCount(str);
        Span<byte> bytes = byteLen > 4098 ? (rented = ArrayPool<byte>.Shared.Rent(byteLen)).AsSpan(0, byteLen) : stackalloc byte[byteLen];
        Span<byte> hash = stackalloc byte[SHA512.HashSizeInBytes];
        Encoding.UTF8.GetBytes(str, bytes);

        try
        {
            SHA512.HashData(bytes, hash);
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

    public static int TryHashToSHA512(this byte[] bytes, Span<byte> output, bool allowTruncation = false)
        => TryHashToSHA512(bytes.AsSpan(), output, allowTruncation);

    public static int TryHashToSHA512(this Span<byte> bytes, Span<byte> output, bool allowTruncation = false)
        => TryHashToSHA512((ReadOnlySpan<byte>)bytes, output, allowTruncation);

    public static int TryHashToSHA512(this ReadOnlySpan<byte> bytes, Span<byte> output, bool allowTruncation = false)
    {
        if (SHA512.HashSizeInBytes > output.Length && allowTruncation)
        {
            Span<byte> hash = stackalloc byte[SHA512.HashSizeInBytes];
            int written = SHA512.HashData(bytes, hash);
            for (int i = 0; i < output.Length; i++) output[i] = hash[i];
            return hash.Length;
        }

        return SHA512.HashData(bytes, output);
    }

    public static int TryHashToSHA512(this string str, Span<byte> output, bool allowTruncation = false)
        => TryHashToSHA512(str.AsSpan(), output, allowTruncation);

    public static int TryHashToSHA512(this Span<char> str, Span<byte> output, bool allowTruncation = false)
        => TryHashToSHA512((ReadOnlySpan<char>)str, output, allowTruncation);

    public static int TryHashToSHA512(this ReadOnlySpan<char> str, Span<byte> output, bool allowTruncation = false)
    {
        byte[]? rented = null;

        var byteLen = Encoding.UTF8.GetByteCount(str);
        Span<byte> bytes = byteLen > 4098 ? (rented = ArrayPool<byte>.Shared.Rent(byteLen)).AsSpan(0, byteLen) : stackalloc byte[byteLen];
        Encoding.UTF8.GetBytes(str, bytes);

        try
        {
            return TryHashToSHA512(bytes, output, allowTruncation);
        }
        finally
        {
            if (rented != null)
                ArrayPool<byte>.Shared.Return(rented);
        }
    }

    #endregion
}
