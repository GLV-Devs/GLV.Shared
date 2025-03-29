using System.Diagnostics;
using System.Text.RegularExpressions;

namespace GLV.Shared.ChatBot.Discord;

internal static partial class DiscordRegexes
{
    private static readonly Dictionary<string, Regex> regexes = [];

    public static Regex CheckCommandRegex(string botHandle, bool compiled = true)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(botHandle);

        if (regexes.TryGetValue(botHandle, out var regex))
        {
            Debug.Assert(regex is not null);
            return regex;
        }

        var options = RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.NonBacktracking;
        if (compiled)
            options |= RegexOptions.Compiled;

        return regexes[botHandle] = new Regex($"^[!/]?(?<cmd>\\w+)({botHandle})?$", options);
    }
}
