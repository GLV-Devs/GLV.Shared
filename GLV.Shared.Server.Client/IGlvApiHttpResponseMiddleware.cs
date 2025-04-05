namespace GLV.Shared.Server.Client;

public interface IGlvApiHttpResponseMiddleware
{
    public ValueTask InterceptResponseAsync(HttpResponseMessage response, CancellationToken cancellationToken);
}
