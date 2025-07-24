using System.Buffers;
using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.RegularExpressions;

namespace GLV.Shared.Common;

public static partial class StringExtensions
{
    [return: NotNullIfNotNull(nameof(addr))]
    public static string? GetPropertyName(this string addr)
    {
        var m = PropertyNameRegex.Match(addr);
        return m.Success ? m.Groups["prop"].Value : addr;
    }

    private static readonly Dictionary<Type, string> typeExpressionCache = [];
    
    public static string GetCSharpTypeExpression(this Type type, bool noCache = false, StringBuilder? sb = null)
    {
        if (type.IsGenericType is false || type.IsConstructedGenericType is false) return type.Name;

        if (noCache is false && typeExpressionCache.TryGetValue(type, out var str) is true)
            return str;
        
        sb ??= new StringBuilder(100);
        var genTypeDef = type.GetGenericTypeDefinition();
        sb.Append(genTypeDef.Name).Append('<');
        foreach (var gt in type.GenericTypeArguments)
            sb.Append(gt.GetCSharpTypeExpression(sb: sb)).Append(", ");
        sb.Remove(sb.Length - 2, 2).Append('>');
        str = sb.ToString();
        
        if (noCache is false)
            typeExpressionCache[type] = str;

        return str;
    }

    [GeneratedRegex(@"(?<prop>\w+)$", RegexOptions.Singleline)]
    private static partial Regex PropertyNameRegex { get; }

    public static StringBuilder AppendTabs(this StringBuilder sb, int tabs)
    {
        ArgumentNullException.ThrowIfNull(sb);
        if (tabs <= 0) return sb;
        for (int i = 0; i < tabs; i++)
            sb.Append('\t');

        return sb;
    }
    
    public static Span<char> ToStringSpan<T>(this T obj, Span<char> buffer, ReadOnlySpan<char> format = default, IFormatProvider? provider = null)
        where T : ISpanFormattable
    {
        if (obj.TryFormat(buffer, out int written, format, provider))
            return buffer[..written];

        throw new ArgumentException("The buffer is not large enough to complete the format operation");
    }

    public static bool TryToStringSpan<T>(this T obj, Span<char> buffer, out Span<char> result, ReadOnlySpan<char> format = default, IFormatProvider? provider = null)
        where T : ISpanFormattable
    {
        if (obj.TryFormat(buffer, out int written, format, provider))
        {
            result = buffer[..written];
            return true;
        }

        result = buffer;
        return false;
    }
    
    public static bool TryGetTextAfter(this string str, string c, [NotNullWhen(true)] out string? text)
    {
        var firstSpace = str.IndexOf(c);
        if (firstSpace == -1 || firstSpace + 1 >= str.Length)
        {
            text = null;
            return false;
        }

        text = str[(firstSpace + c.Length)..];
        return true;
    }

    public static bool TryGetTextAfter(this string str, char c, [NotNullWhen(true)] out string? text)
    {
        var firstSpace = str.IndexOf(c);
        if (firstSpace == -1 || firstSpace + 1 >= str.Length)
        {
            text = null;
            return false;
        }

        text = str[(firstSpace + 1)..];
        return true;
    }

    public static string ToStringOrDefault<T>(this Nullable<T> value, string @default, string? format = null, IFormatProvider? formatProvider = null)
        where T : struct, IFormattable 
        => value is null ? @default : value.Value.ToString(format, formatProvider);

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
        => ToHexViaLookup32((ReadOnlySpan<byte>)bytes.AsSpan(startIndex, length));

    public static string ToHexViaLookup32(this byte[] bytes, int startIndex)
        => ToHexViaLookup32((ReadOnlySpan<byte>)bytes.AsSpan(startIndex));

    public static string ToHexViaLookup32(this byte[] bytes)
        => ToHexViaLookup32((ReadOnlySpan<byte>)bytes.AsSpan());

    public static string ToHexViaLookup32(this Span<byte> bytes)
        => ToHexViaLookup32((ReadOnlySpan<byte>)bytes);

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
