namespace GLV.Shared.Server.Client;

public interface IGlvApiHttpRequestMiddleware
{
    public ValueTask InterceptRequestAsync(HttpRequestMessage request, CancellationToken cancellationToken);
}
