using System.Net;

namespace GLV.Shared.Common;

public sealed class SoftwareInfo
{
    private DateTime nextInfoStringRequest = default;

    public static SoftwareInfo GeCopyRightNoticeFor(string clientName)
        => new(
            "https://raw.githubusercontent.com/DiegoG1019/DiegoG1019/main/info/ClientCopyRight.txt",
            $"This website and all its content excluding 3rd party logos, icons and content belong to '{clientName}', © {DateTime.Now.Year} All rights reserved.",
            str => str.Replace("{ClientName}", clientName).Replace("{Year}", DateTime.Now.Year.ToString())
        );

    public static SoftwareInfo GetOwnedByDevCopyRightNoticeFor(string clientName)
        => new(
            "https://raw.githubusercontent.com/DiegoG1019/DiegoG1019/main/info/OwnedByDevCopyRight.txt",
            $"This website's build artifacts, source code, and all its content excluding 3rd party logos, icons and content owned by '{clientName}' belong to Diego García, © {DateTime.Now.Year} All rights reserved.",
            str => str.Replace("{ClientName}", clientName, StringComparison.OrdinalIgnoreCase)
                      .Replace("{Year}", DateTime.Now.Year.ToString(), StringComparison.OrdinalIgnoreCase)
        );

    public static SoftwareInfo CopyRightNotice { get; }
        = new(
            "https://raw.githubusercontent.com/DiegoG1019/DiegoG1019/main/info/CopyRight.txt",
            $"This website, and all its content excluding 3rd party logos and icons belong to Diego García, © {DateTime.Now.Year} All rights reserved.",
            str => str.Replace("{Year}", DateTime.Now.Year.ToString())
        );

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

    public string DefaultInfoString { get; }
    public string InfoStringRawTextUri { get; }
    private string? infoCache;
    private Func<string, string>? Formatter { get; }

    public SoftwareInfo(string infoStringRawTextUri, string defaultInfoString, Func<string, string>? formatter = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(infoStringRawTextUri);
        ArgumentException.ThrowIfNullOrWhiteSpace(defaultInfoString);

        InfoStringRawTextUri = infoStringRawTextUri;
        DefaultInfoString = defaultInfoString;
        Formatter = formatter;
    }

    public string GetDefaultInfoStringIfNotCached()
        => string.IsNullOrWhiteSpace(infoCache) ? DefaultInfoString : infoCache;

    public string GetInfoString()
        => GetInfoStringAsync().Preserve().Result;

    public async ValueTask<string> GetInfoStringAsync()
    {
        if (infoCache is null || DateTime.Now > nextInfoStringRequest)
        {
            try
            {
                using HttpClient client = new();
                using var msg = await client.GetAsync(InfoStringRawTextUri);
                if (msg.IsSuccessStatusCode is false)
                    infoCache = DefaultInfoString;
                else
                {
                    var str = await msg.Content.ReadAsStringAsync();
                    infoCache = Formatter is not null ? Formatter.Invoke(str) : str;
                }

                nextInfoStringRequest = DateTime.Now + TimeSpan.FromHours(6);
            }
            catch
            {
                infoCache = DefaultInfoString;
                nextInfoStringRequest = DateTime.Now + TimeSpan.FromMinutes(10);
            }
        }

        return infoCache;
    }
}
