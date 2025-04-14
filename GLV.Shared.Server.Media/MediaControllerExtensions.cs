using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using System.Buffers;
using System.Collections.Frozen;
using System.Diagnostics;
using System.Reflection;
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

    public static WebApplication AddGlvMediaEndpoints(this WebApplication app, string baseAddress = "api/media", string endpointTag = "GLV Media")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(baseAddress);

        app.MapGet($"{baseAddress}/svg", (HttpContext context) => context.Response.WriteAsJsonAsync(SVGs.Keys)).WithTags(endpointTag);

        var cacheOutputMetadata = new ResponseCacheAttribute
        {
            Duration = (int)TimeSpan.FromDays(1).TotalSeconds,
            Location = ResponseCacheLocation.Any,
            NoStore = false
        };

        foreach (var (addr, svg) in SVGs)
        {
            app.MapGet($"{baseAddress}/svg/{addr}", async (HttpContext context, [FromQuery] string? color) =>
            {
                var resp = context.Response;
                resp.ContentType = "image/svg+xml";
                
                var dat = svg.GetSVG(color);
                int len;
                resp.ContentLength = len = Encoding.UTF8.GetByteCount(dat);

                byte[] buffer = ArrayPool<byte>.Shared.Rent(len);
                var tgbresult = Encoding.UTF8.TryGetBytes(dat, buffer, out int written);
                Debug.Assert(tgbresult);
                await resp.Body.WriteAsync(buffer.AsMemory(0, written));
            }).WithTags(endpointTag)
              .WithMetadata(cacheOutputMetadata)
              .CacheOutput();
        }

        return app;
    }
}