using System.Diagnostics.CodeAnalysis;

namespace GLV.Shared.Server.Media;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
internal sealed class MediaNameAttribute(string mediaName) : Attribute
{
    [field: AllowNull]
    public string MediaName
    {
        get;
        init
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(value);
            field = value;
        }
    } = mediaName;
}
