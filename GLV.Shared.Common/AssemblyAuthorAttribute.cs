using System.Text.RegularExpressions;

namespace GLV.Shared.Common;

[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false, Inherited = false)]
public sealed partial class AssemblyAuthorAttribute(string AuthorString) : Attribute
{
    public string AuthorString { get; } = AuthorString ?? throw new ArgumentNullException(nameof(AuthorString));

    public string[] Authors => AuthorRegex().Split(AuthorString);

    [GeneratedRegex(@"\s*;\s*")]
    private static partial Regex AuthorRegex();
}
