using System.Buffers;
using System.Security.Cryptography;
using System.Text;

namespace GLV.Shared.Common;

public static class SHA512Extensions
{
    public static string ToSHA512(this byte[] bytes)
        => ToSHA512(bytes.AsSpan());

    public static string ToSHA512(this Span<byte> bytes)
        => ToSHA512((ReadOnlySpan<byte>)bytes);

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
}
