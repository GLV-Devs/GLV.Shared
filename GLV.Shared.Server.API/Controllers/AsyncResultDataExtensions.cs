using GLV.Shared.DataTransfer;

namespace GLV.Shared.Server.API.Controllers;

public static class AsyncResultDataExtensions
{
    public static IServerResponse CreateServerResponse(this AsyncResultData data, string traceIdentifier)
        => BuildServerAsyncResponse(data, traceIdentifier);

    private sealed class ServerAsyncResponse<T>(string? dataType, string traceId, IAsyncEnumerable<T>? data) : IServerResponse
    {
        public string? DataType => dataType;

        public string TraceId { get; set; } = traceId ?? throw new ArgumentNullException(nameof(traceId));

        public IAsyncEnumerable<T>? Data { get; set; } = data;
    }

    [ThreadStatic]
    private static Type[]? ServerAsyncResponseGenericTypeBuffer;

    [ThreadStatic]
    private static object[]? ServerAsyncResponseConstructorParametersBuffer;

    private static IServerResponse BuildServerAsyncResponse(AsyncResultData data, string traceId)
    {
        var typeArgs = ServerAsyncResponseGenericTypeBuffer ??= [null!];
        typeArgs[0] = data.AsyncEnumerableType;

        var ctorParams = ServerAsyncResponseConstructorParametersBuffer ?? [null!, null!, null!];
        ctorParams[0] = data.AsyncEnumerableType.Name;
        ctorParams[1] = traceId;
        ctorParams[2] = data.Data!;

        var inst = Activator.CreateInstance(typeof(ServerAsyncResponse<>).MakeGenericType(typeArgs), ctorParams)!;
        Array.Clear(ctorParams);
        return (IServerResponse)inst;
    }
}
