using System.Collections;
using System.Net;
using System.Text.Json;
using GLV.Shared.Data;
using GLV.Shared.Data.JsonConverters;

namespace GLV.Shared.Server.Client.Models;

public sealed class RequestResponse
{
    public string? DataType { get; set; }

    public string? TraceId { get; set; }

    public IEnumerable? Data { get; set; }
}

public static class RequestResponseExtensions
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    static RequestResponseExtensions()
    {
        JsonSerializerOptions.Converters.Add(SnowflakeConverter.Instance);
    }

    public static IEnumerable<T> GetData<T>(this RequestResponse response)
        => response is { Data: null } or { DataType: null or "" }
            ? Array.Empty<T>()
            : response.Data.Cast<JsonElement>().Select(x => x.Deserialize<T>(JsonSerializerOptions)!);

    public static T? GetSingleData<T>(this RequestResponse response) 
        => response is { Data: null } or { DataType: null or "" }
                ? default
                : response.Data.Cast<JsonElement>().Select(x => x.Deserialize<T>(JsonSerializerOptions)!).Single();

    public static IEnumerable<ErrorMessage> GetErrors(this RequestResponse? response)
        => response is not null and { Data: not null, DataType: nameof(ErrorList) }
            ? response.Data.Cast<JsonElement>().Select(x => x.Deserialize<ErrorMessage>(JsonSerializerOptions)!)
            : [new ErrorMessage("The server did not return any data", "local_NoResponse", null)];
}