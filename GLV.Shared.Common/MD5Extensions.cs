using System.Buffers;
using System.Security.Cryptography;
using System.Text;

namespace GLV.Shared.Common;

public static class MD5Extensions
{
    #region To String

    public static string ToMD5(this byte[] bytes)
        => ToMD5(bytes.AsSpan());

    public static string ToMD5(this Span<byte> bytes)
        => ToMD5(bytes);

    public static string ToMD5(this ReadOnlySpan<byte> bytes)
    {
        Span<byte> hash = stackalloc byte[MD5.HashSizeInBytes];
        MD5.HashData(bytes, hash);
        return hash.ToHexViaLookup32();
    }

    public static string ToMD5(this string str)
        => ToMD5(str.AsSpan());

    public static string ToMD5(this Span<char> str)
        => ToMD5((ReadOnlySpan<char>)str);

    public static string ToMD5(this ReadOnlySpan<char> str)
    {
        byte[]? rented = null;

        var byteLen = Encoding.UTF8.GetByteCount(str);
        Span<byte> bytes = byteLen > 4098 ? (rented = ArrayPool<byte>.Shared.Rent(byteLen)).AsSpan(0, byteLen) : stackalloc byte[byteLen];
        Span<byte> hash = stackalloc byte[MD5.HashSizeInBytes];
        Encoding.UTF8.GetBytes(str, bytes);

        try
        {
            MD5.HashData(bytes, hash);
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

    public static byte[] ToMD5Array(this byte[] bytes)
        => ToMD5Array(bytes.AsSpan());

    public static byte[] ToMD5Array(this Span<byte> bytes)
        => ToMD5Array(bytes);

    public static byte[] ToMD5Array(this ReadOnlySpan<byte> bytes)
    {
        Span<byte> hash = stackalloc byte[MD5.HashSizeInBytes];
        MD5.HashData(bytes, hash);
        return hash.ToArray();
    }

    public static byte[] ToMD5Array(this string str)
        => ToMD5Array(str.AsSpan());

    public static byte[] ToMD5Array(this Span<char> str)
        => ToMD5Array((ReadOnlySpan<char>)str);

    public static byte[] ToMD5Array(this ReadOnlySpan<char> str)
    {
        byte[]? rented = null;

        var byteLen = Encoding.UTF8.GetByteCount(str);
        Span<byte> bytes = byteLen > 4098 ? (rented = ArrayPool<byte>.Shared.Rent(byteLen)).AsSpan(0, byteLen) : stackalloc byte[byteLen];
        Span<byte> hash = stackalloc byte[MD5.HashSizeInBytes];
        Encoding.UTF8.GetBytes(str, bytes);

        try
        {
            MD5.HashData(bytes, hash);
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

    public static int TryHashToMD5(this byte[] bytes, Span<byte> output)
        => TryHashToMD5(bytes.AsSpan(), output);

    public static int TryHashToMD5(this Span<byte> bytes, Span<byte> output)
        => TryHashToMD5(bytes, output);

    public static int TryHashToMD5(this ReadOnlySpan<byte> bytes, Span<byte> output)
        => MD5.HashData(bytes, output);

    public static int TryHashToMD5(this string str, Span<byte> output)
        => TryHashToMD5(str.AsSpan(), output);

    public static int TryHashToMD5(this Span<char> str, Span<byte> output)
        => TryHashToMD5((ReadOnlySpan<char>)str, output);

    public static int TryHashToMD5(this ReadOnlySpan<char> str, Span<byte> output)
    {
        byte[]? rented = null;

        var byteLen = Encoding.UTF8.GetByteCount(str);
        Span<byte> bytes = byteLen > 4098 ? (rented = ArrayPool<byte>.Shared.Rent(byteLen)).AsSpan(0, byteLen) : stackalloc byte[byteLen];
        Encoding.UTF8.GetBytes(str, bytes);

        try
        {
            return MD5.HashData(bytes, output);
        }
        finally
        {
            if (rented != null)
                ArrayPool<byte>.Shared.Return(rented);
        }
    }

    #endregion
}
