using System.Net.Http.Json;
using System.Net;
using System.Text.Json;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ObjectPool;
using GLV.Shared.Server.Client.Models;

namespace GLV.Shared.Server.Client;

public class GlvClientModule<TClient>(TClient client)
    where TClient : GlvApiClientBase
{
    public TClient Client { get; } = client ?? throw new ArgumentNullException(nameof(client));
    protected ILogger? Logger => Client.Logger;
    protected HttpClient Http => Client.Http;
    protected ObjectPool<StringBuilder> StringBuilderPool => Client.StringBuilderPool;

    protected Task<RequestResponse?> GetRequestResponse(HttpResponseMessage resp)
        => Client.GetRequestResponse(resp);

    protected Task<ResponseResultData<T>> ProcessSingleData<T>(HttpResponseMessage resp)
        => Client.ProcessSingleData<T>(resp);
    
    protected Task<ResponseResultData<IEnumerable<T>>> ProcessData<T>(HttpResponseMessage resp)
        => Client.ProcessData<T>(resp);

    protected Task<ResponseResult> ProcessNoData(HttpResponseMessage resp)
        => Client.ProcessNoData(resp);
    
    protected Task<ResponseResultData<T>> ProcessSingleData<T>(Task<HttpResponseMessage> respTask)
        => Client.ProcessSingleData<T>(respTask);
    
    protected Task<ResponseResultData<IEnumerable<T>>> ProcessData<T>(Task<HttpResponseMessage> respTask)
        => Client.ProcessData<T>(respTask);

    protected Task<ResponseResult> ProcessNoData(Task<HttpResponseMessage> respTask)
        => Client.ProcessNoData(respTask);

    protected Task<ResponseResultData<T>> GetSingle<T>(
        string uri,
        IQueryModel? query = null,
        HttpCompletionOption completion = HttpCompletionOption.ResponseContentRead
    ) => Client.GetSingle<T>(uri, query, completion);

    protected Task<ResponseResultData<IEnumerable<T>>> GetMulti<T>(
        string uri,
        IQueryModel? query = null,
        HttpCompletionOption completion = HttpCompletionOption.ResponseContentRead
    ) => Client.GetMulti<T>(uri, query, completion);

    protected Task<ResponseResultData<TData>> Update<TData, TValue>(
        string uri,
        TValue model,
        IQueryModel? query = null,
        HttpCompletionOption completion = HttpCompletionOption.ResponseContentRead
    ) => Client.Update<TData, TValue>(uri, model, query, completion);

    protected Task<ResponseResult> CreateNoReturn<TValue>(
        string uri,
        TValue model,
        IQueryModel? query = null,
        HttpCompletionOption completion = HttpCompletionOption.ResponseContentRead
    ) => Client.CreateNoReturn(uri, model, query, completion);

    protected Task<ResponseResultData<TData>> Create<TData, TValue>(
        string uri,
        TValue model,
        IQueryModel? query = null,
        HttpCompletionOption completion = HttpCompletionOption.ResponseContentRead
    ) => Client.Create<TData, TValue>(uri, model, query, completion);

    protected Task<ResponseResult> PatchNoReturn<TValue>(
        string uri,
        TValue model,
        IQueryModel? query = null,
        HttpCompletionOption completion = HttpCompletionOption.ResponseContentRead
    ) => Client.UpdateNoReturn(uri, model, query, completion);

    protected Task<ResponseResult> Delete(
        string uri, 
        IQueryModel? query = null, 
        HttpCompletionOption completion = HttpCompletionOption.ResponseContentRead
    )
        => Client.Delete(uri, query, completion);

    protected internal Task<ResponseResultData<TData>> Put<TData, TValue>(
        string uri,
        TValue model,
        IQueryModel? query = null,
        HttpCompletionOption completion = HttpCompletionOption.ResponseContentRead
    ) => Client.Put<TData, TValue>(uri, model, query, completion);

    protected internal Task<ResponseResult> PutNoReturn<TValue>(
        string uri,
        TValue model,
        IQueryModel? query = null,
        HttpCompletionOption completion = HttpCompletionOption.ResponseContentRead
    ) => Client.PutNoReturn(uri, model, query, completion);

    protected internal Task<ResponseResultData<HttpContent>> ProcessDataBlob(HttpResponseMessage resp)
        => Client.ProcessDataBlob(resp);

    protected internal Task<ResponseResultData<HttpContent>> GetDataBlob(
        string uri,
        IQueryModel? query = null,
        HttpCompletionOption completion = HttpCompletionOption.ResponseHeadersRead,
        HttpMethod? method = null
    ) => Client.GetDataBlob(uri, query, completion, method);
}
