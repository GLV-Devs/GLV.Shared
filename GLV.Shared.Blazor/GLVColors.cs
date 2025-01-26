using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Drawing;
using System.Text;

namespace GLV.Shared.Blazor;

public static class GLVColors
{
    static GLVColors()
    {
        _RgbColorDict = StandardColors.ToFrozenDictionary(
            k => k,
            v => v.Core_GetWebRGBString()
        );

        _RgbaColorDict = StandardColors.ToFrozenDictionary(
            k => k,
            v => v.Core_GetWebRGBAString()
        );
    }

    private static readonly FrozenDictionary<Color, string> _RgbColorDict;
    private static readonly FrozenDictionary<Color, string> _RgbaColorDict;

    public static ImmutableArray<Color> StandardColors { get; } = [
        Color.FromArgb(192, 0, 0),
        Color.FromArgb(254, 0, 0),
        Color.FromArgb(255, 192, 0),
        Color.FromArgb(255, 255, 0),
        Color.FromArgb(146, 209, 79),
        Color.FromArgb(0, 175, 80),
        Color.FromArgb(1, 176, 241),
        Color.FromArgb(0, 113, 193),
        Color.FromArgb(1, 32, 96),
        Color.FromArgb(112, 48, 160)
    ];

    /// <summary>
    /// Appends the HLS color in CSS format
    /// </summary>
    /// <param name="builder">The string builder to append it to</param>
    /// <param name="hue">The hue parameter, from 0.0 to 1.0</param>
    /// <param name="saturation">The saturation parameter, from 0.0 to 1.0</param>
    /// <param name="lightness">The lightness parameter, from 0.0 to 1.0</param>
    public static StringBuilder AppendHlsColor(this StringBuilder builder, float hue, float saturation, float lightness)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Append("hsl(");

        Span<char> buffer = stackalloc char[20];
        hue.TryFormat(buffer, out var written, "##0.0");
        builder.Append(buffer[..written]).Append(',');

        saturation.TryFormat(buffer, out written, ".##%");
        builder.Append(buffer[..written]).Append(',');

        lightness.TryFormat(buffer, out written, ".##%");
        return builder.Append(buffer[..written]).Append(')');
    }
    public static StringBuilder AppendHlsColor(this StringBuilder builder, Color color)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Append("hsl(");

        Span<char> buffer = stackalloc char[20];
        color.GetHue().TryFormat(buffer, out var written, "##0.0");
        builder.Append(buffer[..written]).Append(',');

        color.GetSaturation().TryFormat(buffer, out written, ".##%");
        builder.Append(buffer[..written]).Append(',');

        color.GetBrightness().TryFormat(buffer, out written, ".##%");
        return builder.Append(buffer[..written]).Append(')');
    }

    private static string Core_GetWebRGBString(this Color color)
        => $"rgb({color.R}, {color.G}, {color.B})";

    public static string GetWebRGBString(this Color color)
        => _RgbColorDict.TryGetValue(color, out var str) ? str : Core_GetWebRGBString(color);

    private static string Core_GetWebRGBAString(this Color color)
        => $"rgba({color.R}, {color.G}, {color.B}, {color.A})";

    public static string GetWebRGBAString(this Color color)
        => _RgbaColorDict.TryGetValue(color, out var str) ? str : Core_GetWebRGBAString(color);
}
