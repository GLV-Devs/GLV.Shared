using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using System.Buffers;
using System.Collections.Frozen;
using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics.Arm;
using System.Text;

namespace GLV.Shared.Server.Media;

public static class MediaControllerExtensions
{
    private readonly static FrozenDictionary<string, SVGIcon> SVGs;

    static MediaControllerExtensions()
    {
        SVGs = typeof(SVGMedia).GetProperties()
                               .Select(x => (x, x.GetCustomAttribute<MediaNameAttribute>()))
                               .Where(x => x.Item2 is not null)
                               .ToFrozenDictionary(
                                   k => k.Item2!.MediaName,
                                   v => (SVGIcon)v.x.GetValue(null)!,
                                   StringComparer.OrdinalIgnoreCase
                               );
    }

    /// <summary>
    /// Adds endpoints that deliver media 
    /// </summary>
    /// <param name="baseAddress">The base address for the media endpoints. Defaults to <c>api/media</c></param>
    /// <param name="endpointTag">The Swagger/OpenApi tag for these endpoints. Defaults to <c>GLV Media</c></param>
    /// <param name="includePreview">Whether or not to render and include special preview html pages. If <see langword="null"/>, will assume <see langword="true"/> only in <c>DEBUG</c> builds, otherwise <see langword="false"/>. Otherwise will respect values <see langword="false"/> and <see langword="true"/></param>
    /// <returns></returns>
    public static WebApplication AddGlvMediaEndpoints(
        this WebApplication app, 
        string baseAddress = "api/media", 
        string endpointTag = "GLV Media",
        bool? includePreview = null
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(baseAddress);

#if DEBUG
        includePreview ??= true;
#endif

        StringBuilder? html = null;
        if (includePreview is true)
        {
            html = new StringBuilder(2000);
            html.Append("<!doctype html><html><head><style>div { margin: 1rem; padding: 1rem; border: 1px solid black; display: flex; justify-content: center; }</style></head><body>");
        }

        app.MapGet($"{baseAddress}/svg", (HttpContext context) => context.Response.WriteAsJsonAsync(SVGs.Keys)).WithTags(endpointTag);

        var cacheOutputMetadata = new ResponseCacheAttribute
        {
            Duration = (int)TimeSpan.FromDays(1).TotalSeconds,
            Location = ResponseCacheLocation.Any,
            NoStore = false
        };

        foreach (var (addr, svg) in SVGs)
        {
            html?.Append("<div><img src=\"").Append(addr).Append("\"/><p>").Append(addr).Append("</p></div>");

            app.MapGet($"{baseAddress}/svg/{addr}", async (HttpContext context, [FromQuery] string? color) =>
            {
                var resp = context.Response;
                resp.ContentType = "image/svg+xml";
                
                var dat = svg.GetSVG(color);
                int len;
                resp.ContentLength = len = Encoding.UTF8.GetByteCount(dat);

                byte[] buffer = ArrayPool<byte>.Shared.Rent(len);
                try
                {
                    var tgbresult = Encoding.UTF8.TryGetBytes(dat, buffer, out int written);
                    Debug.Assert(tgbresult);
                    await resp.Body.WriteAsync(buffer.AsMemory(0, written));
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(buffer);
                }
            }).WithTags(endpointTag)
              .WithMetadata(cacheOutputMetadata)
              .CacheOutput();
        }

        html?.Append("</body>");
        if (html is not null)
        {
            var preview = html.ToString();
            app.MapGet($"{baseAddress}/svg/svg-preview", async(HttpContext context) =>
            {
                var resp = context.Response;
                resp.ContentType = "text/html; charset=utf-8";

                int len;
                resp.ContentLength = len = Encoding.UTF8.GetByteCount(preview);

                byte[] buffer = ArrayPool<byte>.Shared.Rent(len);
                try
                {
                    var tgbresult = Encoding.UTF8.TryGetBytes(preview, buffer, out int written);
                    Debug.Assert(tgbresult);
                    await resp.Body.WriteAsync(buffer.AsMemory(0, written));
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(buffer);
                }
            });
        }

        return app;
    }
}