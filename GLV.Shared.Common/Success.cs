using System.Diagnostics.CodeAnalysis;
using System.Diagnostics;

namespace GLV.Shared.Common;

public readonly struct Success<T>
{
    public Success(T result)
    {
        Debug.Assert(result is not null, "result was unexpectedly null despite being a succesful Result");
        Result = result;
        IsSuccess = true;
    }

    public Success()
    {
        IsSuccess = false;
        Result = default!;
    }

    public static Success<T> Failure => default;

    public T Result { get; }

    [MemberNotNullWhen(true, nameof(Result))]
    public bool IsSuccess { get; }

    public static implicit operator Success<T>(T value)
        => new(value);

    public bool TryGetResult([NotNullWhen(true)][MaybeNullWhen(false)] out T result)
    {
        if (IsSuccess)
        {
            Debug.Assert(Result is not null, "Result is null despite IsSuccess being true");
            result = Result;
            return true;
        }

        result = default;
        return false;
    }
}
