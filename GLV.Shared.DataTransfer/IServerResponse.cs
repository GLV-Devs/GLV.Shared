using GLV.Shared.Data;

namespace GLV.Shared.DataTransfer;

public interface IServerResponse
{
    public string? DataType { get; }
    public string TraceId { get; }
}
