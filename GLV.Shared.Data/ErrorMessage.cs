using System.Net;
using System.Runtime.CompilerServices;

namespace GLV.Shared.Data;

public struct ErrorList(HttpStatusCode recommendedCode, bool forceEmpty = false)
{
    internal List<ErrorMessage>? _errors;
    internal readonly bool forceEmpty = forceEmpty;

    public readonly int Count => _errors?.Count ?? 0;

    public readonly IEnumerable<ErrorMessage> Errors => _errors ?? (IEnumerable<ErrorMessage>)Array.Empty<ErrorMessage>();

    public HttpStatusCode? RecommendedCode { get; set; } = recommendedCode;
}

public readonly record struct ErrorMessageProperty(string Key, string? Value)
{
    public static ErrorMessageProperty Create<T>(T value, [CallerArgumentExpression(nameof(value))] string key = "")
        => new(key, value?.ToString());
}

public readonly record struct ErrorMessage(string? DefaultMessageEN, string Key, IEnumerable<ErrorMessageProperty>? Properties);
