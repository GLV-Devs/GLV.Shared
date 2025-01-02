using System.Text.RegularExpressions;

namespace GLV.Shared.ChatBot;

public static partial class CommandHelpers 
{
    public static IEnumerable<string> SplitArguments(string message)
        => ArgumentsRegex.Split(message).Where(s => !string.IsNullOrWhiteSpace(s));

    public static Regex ArgumentsRegex { get; } = SeparateArgsRegex();

    [GeneratedRegex("\"([^\"]*)\"|(\\s+)")]
    private static partial Regex SeparateArgsRegex();
}
