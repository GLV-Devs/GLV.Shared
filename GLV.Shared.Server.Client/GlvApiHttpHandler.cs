using GLV.Shared.Server.Client.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using static System.Collections.Specialized.BitVector32;

namespace GLV.Shared.Server.Client;

public class GlvApiHttpHandler(
    GlvApiClientBase client,
    IEnumerable<IGlvApiHttpRequestMiddleware>? requestMiddleware,
    IEnumerable<IGlvApiHttpResponseMiddleware>? responseMiddleware,
    HttpMessageHandler? innerHandler = null
) : DelegatingHandler(innerHandler ?? new HttpClientHandler())
{
    private readonly SemaphoreSlim Semaphore = new(1, 1);
    private DateTime LastRefreshed = default;
    private int unauthorizedRetryAttempts = 2;

    private readonly IGlvApiHttpRequestMiddleware[] RequestMiddleware = requestMiddleware?.ToArray() ?? [];
    private readonly IGlvApiHttpResponseMiddleware[] ResponseMiddleware = responseMiddleware?.ToArray() ?? [];

    public int UnauthorizedRetryAttempts
    {
        get => unauthorizedRetryAttempts;
        set
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(value, 1);
            unauthorizedRetryAttempts = value;
        }
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        foreach (var reqMid in RequestMiddleware)
            await reqMid.InterceptRequestAsync(request, cancellationToken);

        var resp = await SendMessage(request, cancellationToken);

        foreach (var resMid in ResponseMiddleware)
            await resMid.InterceptResponseAsync(resp, cancellationToken);

        return resp;
    }

    private async Task<HttpResponseMessage> SendMessage(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        HttpResponseMessage? response;

        if (string.IsNullOrWhiteSpace(client.SessionToken))
            return await base.SendAsync(request, cancellationToken);
        else
        {
            int attempts = int.Clamp(UnauthorizedRetryAttempts, 1, int.MaxValue - 1) + 1;

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", client.SessionToken);
            response = await base.SendAsync(request, cancellationToken);

            if (string.IsNullOrWhiteSpace(client.RefreshToken) || response.StatusCode is not HttpStatusCode.Unauthorized)
                return response;

            while (attempts-- > 0)
            {
                DateTime blocked = DateTime.Now;
                await Semaphore.WaitAsync(cancellationToken);

                try
                {
                    if (blocked > LastRefreshed + TimeSpan.FromSeconds(5))
                    {
                        using var refreshmsg = new HttpRequestMessage(
                            HttpMethod.Patch,
                            new Uri(client.Http.BaseAddress!, client.RefreshRelativeUri)
                        );

                        refreshmsg.Content = JsonContent.Create(new RefreshRequest(client.RefreshToken));

                        var refreshResponse = await base.SendAsync(refreshmsg, cancellationToken);

                        if (refreshResponse.IsSuccessStatusCode is false) break;

                        var tokens = (await refreshResponse.Content.ReadFromJsonAsync<GLVAccessTokenResponse>(cancellationToken: cancellationToken))!;
                        client.SessionToken = tokens.AccessToken;
                        client.RefreshToken = tokens.RefreshToken;
                        client.SessionExpirationDate = DateTimeOffset.FromUnixTimeSeconds(tokens.ExpiresIn);
                        LastRefreshed = DateTime.Now;

                        await client.TryRefreshSession();
                        
                        if (client.SessionStorage?.StoreSession(tokens.RefreshToken) is Task t)
                            await t;
                    }
                }
                finally
                {
                    Semaphore.Release();
                }

                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", client.SessionToken);
                response = await base.SendAsync(request, cancellationToken);
                if (response.StatusCode is not HttpStatusCode.Unauthorized)
                    return response;
            }

            return response;
        }
    }
}
