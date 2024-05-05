using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace GLV.Shared.Data;

public readonly struct SuccessResult
{
    public SuccessResult(ErrorList errors)
    {
        ErrorMessages = errors;
    }

    public ErrorList ErrorMessages { get; }

    public bool IsSuccess => ErrorMessages.Count == 0;

    public static implicit operator SuccessResult(ErrorList errors)
        => new(errors);

    public static SuccessResult Success => new();
}

public readonly struct SuccessResult<T>
{
    public SuccessResult(T result)
    {
        Debug.Assert(result is not null, "result was unexpectedly null despite being a succesful Result");
        Result = result;
        IsSuccess = true;
    }

    public SuccessResult(ErrorList errors)
    {
        IsSuccess = false;
        Result = default!;
        ErrorMessages = errors;
    }

    public static SuccessResult<T> Failure => default;

    public ErrorList ErrorMessages { get; }

    public T Result { get; }

    [MemberNotNullWhen(true, nameof(Result))]
    public bool IsSuccess { get; }

    public static implicit operator SuccessResult<T>(T value)
        => new(value);

    public static implicit operator SuccessResult<T>(ErrorList errors)
        => new(errors);

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
