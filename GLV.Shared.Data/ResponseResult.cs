using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net;

namespace GLV.Shared.Data;

public static class ResponseResultDataExtension
{
    public static async Task<ResponseResult> AssertSuccess(this Task<ResponseResult> result, string? errorMessage = null)
        => (await result).AssertSuccess(errorMessage);

    public static async Task<ResponseResultData<T>> AssertSuccess<T>(this Task<ResponseResultData<T>> result, string? errorMessage = null)
        => (await result).AssertSuccess(errorMessage);

    public static async Task<T> GetData<T>(this Task<ResponseResultData<T>> result, string? errorMessage = null)
        => (await result).GetData(errorMessage);
}

public readonly struct ResponseResult(HttpStatusCode code, IEnumerable<ErrorMessage>? errors = null)
{
    public IEnumerable<ErrorMessage>? Errors { get; } = errors;
    public HttpStatusCode StatusCode { get; } = code;
    public bool IsSuccess => Errors is null;

    public ResponseResult AssertSuccess(string? errorMessage = null) 
        => IsSuccess is false 
            ? string.IsNullOrWhiteSpace(errorMessage) 
                ? throw new ResponseResultFailureException(Errors!) 
                : throw new ResponseResultFailureException(errorMessage, Errors!) 
            : this;

    public ResponseResultData<T> PropagateError<T>()
        => IsSuccess
            ? throw new InvalidOperationException("This method cannot be called on a successful ResponseResult")
            : new ResponseResultData<T>(Errors!, StatusCode);
}

public readonly struct ResponseResultData<T>(T data, HttpStatusCode code, IEnumerable<ErrorMessage>? errors = null)
{
    public ResponseResultData(IEnumerable<ErrorMessage> errors, HttpStatusCode code) : this(default!, code, errors) { }

    public IEnumerable<ErrorMessage>? Errors { get; } = errors;
    public T? Data { get; } = data;
    public HttpStatusCode StatusCode { get; } = code;
    public bool IsSuccess => Errors is null;

    public bool TryGetData([NotNullWhen(true)] out T? data)
    {
        if (IsSuccess)
        {
            Debug.Assert(Data is not null);
            data = Data;
            return true;
        }

        data = default;
        return false;
    }

    public T GetData(string? errorMessage = null)
    {
        AssertSuccess(errorMessage);
        Debug.Assert(Data is not null);
        return Data;
    }

    public ResponseResultData<T> AssertSuccess(string? errorMessage = null) 
        => IsSuccess is false 
            ? string.IsNullOrWhiteSpace(errorMessage) 
                ? throw new ResponseResultFailureException(Errors!)
                : throw new ResponseResultFailureException(errorMessage, Errors!)
            : this;

    public ResponseResult PropagateError()
        => IsSuccess
            ? throw new InvalidOperationException("This method cannot be called on a successful ResponseResult")
            : new ResponseResult(StatusCode, Errors!);

    public ResponseResultData<TOther> PropagateError<TOther>() 
        => IsSuccess
            ? throw new InvalidOperationException("This method cannot be called on a successful ResponseResult")
            : new ResponseResultData<TOther>(Errors!, StatusCode);
}
