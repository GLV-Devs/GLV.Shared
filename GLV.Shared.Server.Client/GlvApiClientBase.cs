using GLV.Shared.Server.Client.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ObjectPool;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Json;
using System.Net;
using System.Text;
using System.Text.Json;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace GLV.Shared.Server.Client;

public abstract class GlvApiClientBase<
    TUserCreateModel,
    TUserViewModel,
    TUserKey,
    TUserSessionView,
    TUserLoginModel
>(
    string baseUrl,
    ILogger<GlvApiClientBase> logger,
    IEnumerable<IGlvApiHttpRequestMiddleware>? requestMiddleware = null,
    IEnumerable<IGlvApiHttpResponseMiddleware>? responseMiddleware = null,
    HttpMessageHandler? innerHandler = null
) : GlvApiClientBase(baseUrl, logger, requestMiddleware, responseMiddleware, innerHandler)
    where TUserKey : unmanaged, IEquatable<TUserKey>, IFormattable, IParsable<TUserKey>
    where TUserLoginModel : IGlvIdentityUserLoginModel
    where TUserSessionView : IGlvIdentitySessionView
{
    public TUserSessionView? Session { get; internal set; }

    public virtual Task<ResponseResultData<TUserSessionView>> GetSessionInfo()
        => GetSingle<TUserSessionView>(IdentityRelativeUri);

    public Task<ResponseResultData<TUserViewModel>> CreateAccount(TUserCreateModel createModel)
        => Create<TUserViewModel, TUserCreateModel>(IdentityRelativeUri, createModel);

    public Task<ResponseResult> ChangeUserPermissions(TUserKey userId, ChangeUserPermissionsModel model)
        => UpdateNoReturn($"{IdentityRelativeUri}/permissions/{userId}", model);

    public async Task<ResponseResult> LogIn(TUserLoginModel model)
    {
        await LogOut();
        var response = await Put<GLVAccessTokenResponse, TUserLoginModel>(IdentityRelativeUri, model);

        if (response.TryGetNonNullData(out var data) is false)
            return response.PropagateError();

        SessionToken = data.AccessToken;
        RefreshToken = data.RefreshToken;
        SessionExpirationDate = DateTimeOffset.FromUnixTimeSeconds(data.ExpiresIn);

        var refreshSessionResponse = await TryRefreshSession();
        if (refreshSessionResponse.IsSuccess && SessionStorage?.StoreSession(RefreshToken) is Task t)
            await t;

        return refreshSessionResponse;
    }

    protected internal override async Task<ResponseResult> TryRefreshSession()
    {
        var sessionInfo = await GetSessionInfo();

        if (sessionInfo.TryGetData(out TUserSessionView? sessionData))
        {
            Debug.Assert(RefreshToken is not null);
            if (SessionStorage?.StoreSession(RefreshToken) is Task t)
                await t;

            Session = sessionData;
        }

        return new ResponseResult(sessionInfo.StatusCode);
    }

    public async Task<ResponseResult> LogOut()
    {
        var response = await Delete(IdentityRelativeUri);
        SessionToken = null;
        RefreshToken = null;
        SessionExpirationDate = default;
        
        if (SessionStorage?.DeleteSession() is Task t)
            await t;

        return response;
    }
}

public abstract class GlvApiClientBase
{
    public GlvApiClientBase(
        string baseUrl,
        ILogger<GlvApiClientBase> logger,
        IEnumerable<IGlvApiHttpRequestMiddleware>? requestMiddleware = null,
        IEnumerable<IGlvApiHttpResponseMiddleware>? responseMiddleware = null,
        HttpMessageHandler? innerHandler = null
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(baseUrl);
        if (Uri.IsWellFormedUriString(baseUrl, UriKind.Absolute) is false)
            throw new ArgumentException("The baseUrl must be a well formed absolute Uri string", nameof(baseUrl));

        Http = new(new GlvApiHttpHandler(this, requestMiddleware, responseMiddleware, innerHandler), true)
        {
            BaseAddress = new Uri(baseUrl, UriKind.Absolute)
        };

        Logger = logger;
    }

    protected internal ObjectPool<StringBuilder> StringBuilderPool { get; } = ObjectPool.Create<StringBuilder>();

    public Task<ResponseResult> ChangePassword(ChangeUserPasswordModel model)
        => UpdateNoReturn($"{IdentityRelativeUri}/password", model);

    protected internal abstract Task<ResponseResult> TryRefreshSession();

    protected internal HttpClient Http { get; }
    protected internal ILogger? Logger { get; }

    public ISessionStorage? SessionStorage { get; set; }

    public string? SessionToken { get; internal set; }

    public string? RefreshToken { get; internal set; }

    public DateTimeOffset SessionExpirationDate { get; internal set; }

    public string IdentityRelativeUri
    {
        get;
        set
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(value);
            field = value;
        }
    } = "api/identity";

    public string RefreshRelativeUri
    {
        get;
        set
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(value);
            field = value;
        }
    } = "api/identity/refresh";

    public void DiscardSession()
    {
        SessionToken = null;
        RefreshToken = null;
        Http.DefaultRequestHeaders.Authorization = null;
    }

    public virtual bool CheckIfLoggedIn()
        => string.IsNullOrWhiteSpace(SessionToken);

    protected internal async Task<RequestResponse?> GetRequestResponse(HttpResponseMessage resp)
    {
        Logger?.LogTrace(
            "Obtaining RequestResponse from HttpResponseMessage from {url} that returned with code {status}",
            resp.RequestMessage?.RequestUri?.ToString(),
            resp.StatusCode.ToString()
        );

        if (resp.StatusCode is HttpStatusCode.RequestTimeout or >= (HttpStatusCode)500 and <= (HttpStatusCode)599)
        {
            Logger?.LogError(
                "Could not obtain a correct RequestResponse from an HttpResponseMessage from {url} that returned with code: {code}",
                resp.RequestMessage?.RequestUri?.ToString(),
                resp.StatusCode.ToString()
            );
            return null;
        }

        if (resp.IsSuccessStatusCode)
            return await resp.Content.ReadFromJsonAsync<RequestResponse>();

        Logger?.LogTrace(
            "Attempting to read RequestResponse from a potentially empty HttpResponseMessage from {url} that returned with code: {code}",
            resp.RequestMessage?.RequestUri?.ToString(),
            resp.StatusCode.ToString()
        );

        using var stream = await resp.Content.ReadAsStreamAsync();
        if (stream.Length != 0)
            return JsonSerializer.Deserialize<RequestResponse>(stream, JsonSerializerOptions.Web);

        Logger?.LogError(
            "Could not obtain any data from RequestResponse from HttpResponseMessage from {url} that returned with code: {code}",
            resp.RequestMessage?.RequestUri?.ToString(),
            resp.StatusCode.ToString()
        );
        return null;
    }

    protected internal async Task<ResponseResultData<HttpContent>> ProcessDataBlob(HttpResponseMessage resp)
    {
        Logger?.LogTrace(
            "Obtaining Data Blob from HttpResponseMessage from {url} that returned with code {status}",
            resp.RequestMessage?.RequestUri?.ToString(),
            resp.StatusCode.ToString()
        );

        if (resp.IsSuccessStatusCode) 
            return new(resp.Content, resp.StatusCode);

        Logger?.LogError(
            "Could not obtain a Data Blob from an HttpResponseMessage from {url} that returned with code: {code}",
            resp.RequestMessage?.RequestUri?.ToString(),
            resp.StatusCode.ToString()
        );

        var response = await GetRequestResponse(resp);
        return new(response.GetErrors(), resp.StatusCode);
    }

    protected internal async Task<ResponseResultData<HttpContent>> GetDataBlob(
        string uri,
        IQueryModel? query = null,
        HttpCompletionOption completion = HttpCompletionOption.ResponseHeadersRead,
        HttpMethod? method = null
    )
    {
        method ??= HttpMethod.Get;

        using var msg = new HttpRequestMessage(method, GetUri(uri, query));
        var response = await Http.SendAsync(msg, completion);
        return await ProcessDataBlob(response);
    }

    protected internal async Task<ResponseResultData<T>> ProcessSingleData<T>(HttpResponseMessage resp)
    {
        var response = await GetRequestResponse(resp);
        return resp.IsSuccessStatusCode
            ? new(response is null ? default : response.GetSingleData<T>(), resp.StatusCode)
            : new(response.GetErrors(), resp.StatusCode);
    }

    protected internal async Task<ResponseResultData<IEnumerable<T>>> ProcessData<T>(HttpResponseMessage resp)
    {
        var response = await GetRequestResponse(resp);
        return resp.IsSuccessStatusCode
            ? new(response?.GetData<T>(), resp.StatusCode)
            : new(response.GetErrors(), resp.StatusCode);
    }
    protected internal async Task<ResponseResult> ProcessNoData(HttpResponseMessage resp)
        => resp.IsSuccessStatusCode is false
            ? new ResponseResult(resp.StatusCode, (await GetRequestResponse(resp)).GetErrors())
            : new ResponseResult(resp.StatusCode);

    protected internal async Task<ResponseResultData<T>> ProcessSingleData<T>(Task<HttpResponseMessage> respTask)
        => await ProcessSingleData<T>(await respTask);

    protected internal async Task<ResponseResultData<IEnumerable<T>>> ProcessData<T>(Task<HttpResponseMessage> respTask)
        => await ProcessData<T>(await respTask);

    protected internal async Task<ResponseResult> ProcessNoData(Task<HttpResponseMessage> respTask)
        => await ProcessNoData(await respTask);

    protected string GetUri(string uri, IQueryModel? query)
    {
        var sb = StringBuilderPool.Get();
        try
        {
            var q = query?.ToQueryString(sb);
            return string.IsNullOrWhiteSpace(q) ? uri : q.StartsWith('?') ? $"{uri}{query}" : $"{uri}?{query}";
        }
        finally
        {
            StringBuilderPool.Return(sb);
        }
    }

    protected internal Task<ResponseResultData<T>> GetSingle<T>(
        string uri,
        IQueryModel? query = null,
        HttpCompletionOption completion = HttpCompletionOption.ResponseContentRead
    )
        => ProcessSingleData<T>(Http.GetAsync(GetUri(uri, query)));

    protected internal Task<ResponseResultData<IEnumerable<T>>> GetMulti<T>(
        string uri,
        IQueryModel? query = null,
        HttpCompletionOption completion = HttpCompletionOption.ResponseContentRead
    )
        => ProcessData<T>(Http.GetAsync(GetUri(uri, query)));

    protected internal Task<ResponseResultData<TData>> Update<TData, TValue>(
        string uri,
        TValue model,
        IQueryModel? query = null,
        HttpCompletionOption completion = HttpCompletionOption.ResponseContentRead
    )
        => ProcessSingleData<TData>(Http.PatchAsJsonAsync(GetUri(uri, query), model));

    protected internal Task<ResponseResult> CreateNoReturn<TValue>(
        string uri,
        TValue model,
        IQueryModel? query = null,
        HttpCompletionOption completion = HttpCompletionOption.ResponseContentRead
    )
        => ProcessNoData(Http.PostAsJsonAsync(GetUri(uri, query), model));

    protected internal Task<ResponseResultData<TData>> Create<TData, TValue>(
        string uri,
        TValue model,
        IQueryModel? query = null,
        HttpCompletionOption completion = HttpCompletionOption.ResponseContentRead
    )
        => ProcessSingleData<TData>(Http.PostAsJsonAsync(GetUri(uri, query), model));

    protected internal Task<ResponseResult> UpdateNoReturn<TValue>(
        string uri,
        TValue model,
        IQueryModel? query = null,
        HttpCompletionOption completion = HttpCompletionOption.ResponseContentRead
    )
        => ProcessNoData(Http.PatchAsJsonAsync(GetUri(uri, query), model));

    protected internal Task<ResponseResultData<TData>> Put<TData, TValue>(
        string uri,
        TValue model,
        IQueryModel? query = null,
        HttpCompletionOption completion = HttpCompletionOption.ResponseContentRead
    )
        => ProcessSingleData<TData>(Http.PutAsJsonAsync(GetUri(uri, query), model));

    protected internal Task<ResponseResult> PutNoReturn<TValue>(
        string uri,
        TValue model,
        IQueryModel? query = null,
        HttpCompletionOption completion = HttpCompletionOption.ResponseContentRead
    )
        => ProcessNoData(Http.PutAsJsonAsync(GetUri(uri, query), model));

    protected internal Task<ResponseResult> Delete(
        string uri, 
        IQueryModel? query = null, 
        HttpCompletionOption completion = HttpCompletionOption.ResponseContentRead
    )
        => ProcessNoData(Http.DeleteAsync(GetUri(uri, query)));
}
