using GLV.Shared.Data;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net;

namespace GLV.Shared.Server.Client.Models;

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

public readonly struct ResponseResultData<T>(T? data, HttpStatusCode code, IEnumerable<ErrorMessage>? errors = null)
{
    public ResponseResultData(IEnumerable<ErrorMessage> errors, HttpStatusCode code) : this(default!, code, errors) { }
    public ResponseResultData(HttpStatusCode code) : this(default!, code, default) { }
    
    public IEnumerable<ErrorMessage>? Errors { get; } = errors;
    public T? Data { get; } = data;
    public HttpStatusCode StatusCode { get; } = code;
    public bool IsSuccess => Errors is null;
    public bool DataIsNull => IsSuccess && Data?.Equals(default) is not false; // this evaluates to true if the equality is true or if Data is null

    public bool TryGetNonNullData([NotNullWhen(true)] out T? data)
    {
        if (IsSuccess && DataIsNull is false)
        {
            Debug.Assert(Data is not null);
            data = Data;
            return true;
        }

        data = default;
        return false;
    }

    public bool TryGetData([MaybeNullWhen(false)] out T? data)
    {
        if (IsSuccess)
        {
            Debug.Assert(DataIsNull || Data is not null);
            data = Data;
            return true;
        }

        data = default;
        return false;
    }

    public T? GetData(string? errorMessage = null)
    {
        AssertSuccess(errorMessage);
        Debug.Assert(DataIsNull || Data is not null);
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
