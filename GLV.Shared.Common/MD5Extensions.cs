using System.Buffers;
using System.Security.Cryptography;
using System.Text;

namespace GLV.Shared.Common;

public static class MD5Extensions
{
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
}
