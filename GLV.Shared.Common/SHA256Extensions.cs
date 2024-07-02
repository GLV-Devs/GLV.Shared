using System.Buffers;
using System.Security.Cryptography;
using System.Text;

namespace GLV.Shared.Common;

public static class SHA256Extensions
{
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
}
