using System.Net;

namespace GLV.Shared.Data;

public struct ErrorList(HttpStatusCode recommendedCode)
{
    internal List<ErrorMessage>? _errors;

    public readonly int Count => _errors?.Count ?? 0;

    public readonly IEnumerable<ErrorMessage> Errors => _errors ?? (IEnumerable<ErrorMessage>)[];

    public HttpStatusCode? RecommendedCode { get; set; } = recommendedCode;
}

public static class ErrorListExtensions
{
    public static ref ErrorList AddError(this ref ErrorList list, ErrorMessage message)
    {
        (list._errors ??= new()).Add(message);
        return ref list;
    }

    public static ref ErrorList AddErrorRange(this ref ErrorList list, IEnumerable<ErrorMessage> messages)
    {
        (list._errors ??= new()).AddRange(messages);
        return ref list;
    }

    public static void Clear(this ref ErrorList list)
        => list._errors?.Clear();
}

public readonly record struct ErrorMessageProperty(string Key, string? Value);

public readonly record struct ErrorMessage(string? DefaultMessageES, string Key, IEnumerable<ErrorMessageProperty>? Properties);
