using System.Drawing;

namespace GLV.Shared.Server.Media;

public sealed class SVGIcon(string svg)
{
    public string SVGMarkup { get; } = svg;

    public string GetSVG(string? color = null)
        => string.IsNullOrWhiteSpace(color) ? SVGMarkup : SVGMarkup.Replace("currentColor", color, StringComparison.OrdinalIgnoreCase);
}
