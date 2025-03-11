using System.Net;

namespace GLV.Shared.Common;

public sealed class SoftwareInfo
{
    private DateTime nextWatermarkRequest = default;

    public static SoftwareInfo HtmlDevDiegoGInfo { get; }
        = new SoftwareInfo(
            "https://raw.githubusercontent.com/DiegoG1019/DiegoG1019/main/info/HtmlSiteSignature.txt",
            "Produced by Diego García. Find me at my <a href=\"diegog1019.github.io\">website</a> or contact me through <a href=\"mailto:dagarciam1014@gmail.com\">dagarciam1014@gmail.com</a>"
        );

    public static SoftwareInfo DevDiegoGInfo { get; }
        = new SoftwareInfo(
            "https://raw.githubusercontent.com/DiegoG1019/DiegoG1019/main/info/SiteSignature.txt",
            "Produced by Diego García. Contact me through https://diegog1019.github.io/ or dagarciam1014@gmail.com"
        );

    public string DefaultWatermark { get; }
    public string WatermarkRawTextUri { get; }
    private string? watermarkCache;

    public SoftwareInfo(string watermarkRawTextUri, string defaultWatermark)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(watermarkRawTextUri);
        ArgumentException.ThrowIfNullOrWhiteSpace(defaultWatermark);

        WatermarkRawTextUri = watermarkRawTextUri;
        DefaultWatermark = defaultWatermark;
    }

    public string GetWatermark()
        => GetWatermarkAsync().Preserve().Result;

    public async ValueTask<string> GetWatermarkAsync()
    {
        if (watermarkCache is null || DateTime.Now > nextWatermarkRequest)
        {
            try
            {
                using HttpClient client = new();
                using var msg = await client.GetAsync(WatermarkRawTextUri);
                watermarkCache = msg.IsSuccessStatusCode is false
                    ? DefaultWatermark
                    : await msg.Content.ReadAsStringAsync();

                nextWatermarkRequest = DateTime.Now + TimeSpan.FromHours(6);
            }
            catch
            {
                watermarkCache = DefaultWatermark;
                nextWatermarkRequest = DateTime.Now + TimeSpan.FromMinutes(10);
            }
        }

        return watermarkCache;
    }
}
