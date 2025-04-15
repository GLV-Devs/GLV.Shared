using System.Text.RegularExpressions;

namespace GLV.Shared.Storage;
public static partial class Regexes
{
    [GeneratedRegex(@"(?<=[/\\]){0,}[\w\d]*?\.?[\w\d]+$")]
    public static partial Regex GetFileOrDirectoryNameRegex();

    [GeneratedRegex(@".*(?=[/\\][\w\d]*?\.?[\w\d]+$)")]
    public static partial Regex GetPathWithoutFileOrDirectoryNameRegex();
}
