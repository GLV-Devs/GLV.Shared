using System.Collections;

namespace GLV.Shared.DataTransfer;

public sealed class ServerResponse(string? dataType, string traceId, IEnumerable? data)
{
    public string? DataType => dataType;

    public string TraceId { get; set; } = traceId ?? throw new ArgumentNullException(nameof(traceId));

    public IEnumerable? Data { get; set; } = data;
}
