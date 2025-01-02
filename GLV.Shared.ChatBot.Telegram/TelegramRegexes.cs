using System.Text.RegularExpressions;

namespace GLV.Shared.ChatBot.Telegram;

internal static partial class TelegramRegexes
{
    /// <summary>
    /// "/\w+"
    /// </summary>
    public static Regex CheckCommandRegex { get; } = GetCheckCommandRegex();

    [GeneratedRegex(@"/\w+")]
    private static partial Regex GetCheckCommandRegex();
}