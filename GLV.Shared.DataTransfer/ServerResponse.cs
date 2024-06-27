using System.Collections;
using System.Net;
using GLV.Shared.Data;

namespace GLV.Shared.DataTransfer;

public sealed class ServerResponse(string? dataType, string traceId, IEnumerable? data)
{
    public string? DataType => dataType;

    public string TraceId { get; set; } = traceId ?? throw new ArgumentNullException(nameof(traceId));

    public IEnumerable? Data { get; set; } = data;
}

public static class ServerResponseExtensions
{
    public static IEnumerable<T> GetData<T>(this ServerResponse response)
    {
        ArgumentNullException.ThrowIfNull(response);
        return response is { Data: null } or { DataType: null or "" }
            ? throw new ArgumentException("this ServerResponse did not return data")
            : response.Data.Cast<T>();
    }

    public static T GetSingleData<T>(this ServerResponse response)
    {
        ArgumentNullException.ThrowIfNull(response);
        return response is { Data: null } or { DataType: null or "" }
            ? throw new ArgumentException("this ServerResponse did not return data")
            : response.Data.Cast<T>().Single();
    }

    public static IEnumerable<ErrorMessage> GetErrors(this ServerResponse? response) 
        => response is not null and { Data: not null, DataType: nameof(ErrorList) }
            ? response.Data.Cast<ErrorMessage>()
            : new[] { new ErrorMessage("An unknown error has ocurred", "unknown", null) };
}