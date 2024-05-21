namespace GLV.Shared.Server.API.Controllers;

/// <summary>
/// This class is used exclusively so that ServerResponseFilter can properly handle IAsyncEnumerable results
/// </summary>
public sealed class AsyncResultData
{
    public required object? Data { get; set; }
    public required Type AsyncEnumerableType { get; set; }
}
