using System.Buffers;
using System.Collections.Frozen;
using System.Security.Cryptography;
using System.Text;

namespace GLV.Shared.Common;

public static class StringExtensions
{
    public static bool ContainsAny(this string str, params string[] candidates)
        => ContainsAny(str.AsSpan(), StringComparison.Ordinal, candidates);

    public static bool ContainsAny(this ReadOnlySpan<char> str, params string[] candidates)
        => ContainsAny(str, StringComparison.Ordinal, candidates);

    public static bool ContainsAny(this string str, StringComparison comparisonType, params string[] candidates)
        => ContainsAny(str.AsSpan(), comparisonType, candidates);

    public static bool ContainsAny(this ReadOnlySpan<char> str, StringComparison comparisonType, params string[] candidates)
    {
        for (int i = 0; i < candidates.Length; i++)
            if (str.Contains(candidates[i], comparisonType)) 
                return true;
        return false;
    }

    public static bool ContainsAny(this string str, IEnumerable<string> candidates, StringComparison comparisonType = StringComparison.Ordinal)
        => ContainsAny(str.AsSpan(), candidates);

    public static bool ContainsAny(this ReadOnlySpan<char> str, IEnumerable<string> candidates, StringComparison comparisonType = StringComparison.Ordinal)
    {
        foreach (var candidate in candidates)
            if (str.Contains(candidate, comparisonType))
                return true;
        return false;
    }

    public static bool ContainsAny(this string str, ReadOnlySpan<char> candidates)
        => ContainsAny(str.AsSpan(), candidates);

    public static bool ContainsAny(this ReadOnlySpan<char> str, ReadOnlySpan<char> candidates)
    {
        for (int i = 0; i < str.Length; i++)
            for (int c = 0; c < candidates.Length; c++)
                if (str[i] == candidates[c])
                    return true;
        return false;
    }

    private static readonly uint[] _lookup32 = CreateLookup32();

    private static uint[] CreateLookup32()
    {
        var result = new uint[256];
        for (int i = 0; i < 256; i++)
        {
            string s = i.ToString("X2");
            result[i] = ((uint)s[0]) + ((uint)s[1] << 16);
        }
        return result;
    }

    public static string ToHexViaLookup32(this byte[] bytes, int startIndex, int length)
        => ToHexViaLookup32(bytes.AsSpan(startIndex, length));

    public static string ToHexViaLookup32(this byte[] bytes, int startIndex)
        => ToHexViaLookup32(bytes.AsSpan(startIndex));

    public static string ToHexViaLookup32(this byte[] bytes)
        => ToHexViaLookup32(bytes.AsSpan());

    public static string ToHexViaLookup32(this Span<byte> bytes)
        => ToHexViaLookup32(bytes);

    public static string ToHexViaLookup32(this ReadOnlySpan<byte> bytes)
    {
        var lookup32 = _lookup32;
        Span<char> result = stackalloc char[bytes.Length * 2];
        for (int i = 0; i < bytes.Length; i++)
        {
            var val = lookup32[bytes[i]];
            result[2 * i] = (char)val;
            result[2 * i + 1] = (char)(val >> 16);
        }
        return new string(result);
    }
}
