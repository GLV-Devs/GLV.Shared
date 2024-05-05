using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GLV.Shared.Server.API;
public static class GLVSoftworksInfo
{
    private static DateTime nextWatermarkRequest = default;

    private const string DefaultWatermark = "Produced by GLV Softworks. Contact us through glvsoftworks@gmail.com";
    private static string? watermark;

    public static string GetWatermark()
        => watermark is null ? GetWatermarkAsync().Preserve().Result : watermark;

    public static async ValueTask<string> GetWatermarkAsync()
    {
        if (watermark is null || DateTime.Now > nextWatermarkRequest)
        {
            try
            {
                using HttpClient client = new();
                using var msg = await client.GetAsync("https://raw.githubusercontent.com/DiegoG1019/DiegoG1019/main/info/SiteSignature.txt");
                watermark = msg.IsSuccessStatusCode is false
                    ? DefaultWatermark
                    : await msg.Content.ReadAsStringAsync();

                nextWatermarkRequest = DateTime.Now + TimeSpan.FromHours(6);
            }
            catch
            {
                watermark = DefaultWatermark;
                nextWatermarkRequest = DateTime.Now + TimeSpan.FromMinutes(10);
            }
        }

        return watermark;
    }
}
