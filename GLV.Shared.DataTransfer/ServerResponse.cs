using System.Collections;
using System.Net;
using System.Text.Json;
using GLV.Shared.Data;
using GLV.Shared.Data.JsonConverters;

namespace GLV.Shared.DataTransfer;

public sealed class ServerResponse(string? dataType, string traceId, IEnumerable? data)
{
    public string? DataType { get; set; } = dataType;

    public string TraceId { get; set; } = traceId;

    public IEnumerable? Data { get; set; } = data;

    public static object CreateServerResponse(ErrorList errorList, string traceIdentifier)
        => new ServerResponse(nameof(ErrorList), traceIdentifier, errorList.Errors);

    public static object CreateServerResponse(IEnumerable<ErrorMessage> errors, string traceIdentifier)
        => new ServerResponse(nameof(ErrorList), traceIdentifier, errors);

    public static object CreateServerResponseFromString(string str, string traceIdentifier)
        => new ServerResponse(
            typeof(string).Name,
            traceIdentifier,
            new string[] { str }
        );

    public static object CreateServerResponse(IEnumerable objects, string traceIdentifier)
    {
        var first = objects.Cast<object>().FirstOrDefault();
        return first is null
            ? new ServerResponse(null, traceIdentifier, null)
            : new ServerResponse(first.GetType().Name, traceIdentifier, objects);
    }

    public static object CreateServerResponseFromString(IEnumerable<string> str, string traceIdentifier)
        => new ServerResponse(
            typeof(string).Name,
            traceIdentifier,
            str
        );

    public static object CreateServerResponseFromObject(object obj, string traceIdentifier)
        => new ServerResponse(obj.GetType().Name, traceIdentifier, new[] { obj });
}

public static class ServerResponseExtensions
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    static ServerResponseExtensions()
    {
        JsonSerializerOptions.Converters.Add(SnowflakeConverter.Instance);
    }

    public static IEnumerable<T> GetData<T>(this ServerResponse response)
        => response is { Data: null } or { DataType: null or "" }
            ? throw new ArgumentException("this ServerResponse did not return data")
            : response.Data.Cast<JsonElement>().Select(x => x.Deserialize<T>(JsonSerializerOptions)!);

    public static T GetSingleData<T>(this ServerResponse response) 
        => response is { Data: null } or { DataType: null or "" }
                ? throw new ArgumentException("this ServerResponse did not return data")
                : response.Data.Cast<JsonElement>().Select(x => x.Deserialize<T>(JsonSerializerOptions)!).Single();

    public static IEnumerable<ErrorMessage> GetErrors(this ServerResponse? response)
        => response is not null and { Data: not null, DataType: nameof(ErrorList) }
            ? response.Data.Cast<JsonElement>().Select(x => x.Deserialize<ErrorMessage>(JsonSerializerOptions)!)
            : new[] { new ErrorMessage("The server did not return any data", "local_NoResponse", null) };
}