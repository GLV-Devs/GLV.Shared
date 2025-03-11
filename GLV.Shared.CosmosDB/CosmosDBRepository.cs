using GLV.Shared.Data;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Security.Cryptography;

namespace GLV.Shared.CosmosDB;

public abstract class CosmosDBRepository<TModel, TKey, TView, TCreateModel, TUpdateModel>(
    CosmosClient Client,
    [FromKeyedServices(CosmosDBExtensions.DefaultDatabaseNameServiceKey)] string databaseName
)
    : IRepository<TModel, TKey, TView, TCreateModel, TUpdateModel>
    where TModel : class, IKeyed<TModel, TKey>
    where TKey : notnull
{
    protected readonly CosmosClient Client = Client;
    protected Database? Database { get; private set; }

    [field: AllowNull]
    public string DatabaseName
    {
        get;
        set
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(databaseName);
            field = databaseName;
        }
    }

    public abstract string ContainerName { get; } 
    public abstract string ContainerPartitionKeyName { get; }
    public PartitionKey ContainerPartitionKey => new(ContainerPartitionKeyName);

    public static SuccessResult FailureResult(HttpStatusCode StatusCode)
    {
        var err = new ErrorList(StatusCode);
        err.AddError(
            new ErrorMessage(
                $"An error ocurred in CosmoDB whilst attempting the request: {StatusCode}",
                "CosmosDBError",
                [
                    new ErrorMessageProperty("StatusCode", StatusCode.ToString())
                ]
            )
        );

        return new(err);
    }

    public static SuccessResult<T> ModelFailureResult<T>(HttpStatusCode StatusCode)
    {
        var err = new ErrorList(StatusCode);
        err.AddError(
            new ErrorMessage(
                $"An error ocurred in CosmoDB whilst attempting the request: {StatusCode}",
                "CosmosDBError",
                [
                    new ErrorMessageProperty("StatusCode", StatusCode.ToString())
                ]
            )
        );

        return new(err);
    }

    public static SuccessResult<TModel> ModelFailureResult(HttpStatusCode StatusCode)
    {
        var err = new ErrorList(StatusCode);
        err.AddError(
            new ErrorMessage(
                $"An error ocurred in CosmoDB whilst attempting the request: {StatusCode}",
                "CosmosDBError",
                [
                    new ErrorMessageProperty("StatusCode", StatusCode.ToString())
                ]
            )
        );

        return new(err);
    }

    public RequestChargeTracker? Tracker { get; init; }

    private Container? ___container;

    protected virtual string KeyToString(TKey id)
        => id.ToString() ?? throw new InvalidOperationException("Cannot use a key that has no valid ToString conversion");

    protected async ValueTask<Container> GetContainer()
    {
        if (___container is null)
        {
            var resp = await Client.CreateDatabaseIfNotExistsAsync(DatabaseName);
            Tracker?.Add(resp.RequestCharge);
            Database = (int)resp.StatusCode is >= 200 and <= 299
                ? resp.Database
                : throw new InvalidOperationException($"Could not obtain or create the CosmosDB Database. Error code: {resp.StatusCode}");
            return ___container = await Database.CreateContainerIfNotExistsAsync(ContainerName, ContainerPartitionKeyName);
        }

        return ___container;
    }

    public async ValueTask<SuccessResult<TModel>> Find(TKey id)
    {
        var container = await GetContainer();
        var resp = await container.ReadItemAsync<TModel>(KeyToString(id), ContainerPartitionKey);
        Tracker?.Add(resp.RequestCharge);
        return (int)resp.StatusCode is >= 200 and <= 299 ? new(resp.Resource) : ModelFailureResult(resp.StatusCode);
    }

    public IQueryable<TModel> Get(IEntityQuery<TModel, TKey>? query = null)
    {
        var container = ___container ?? GetContainer().Preserve().ConfigureAwait(false).GetAwaiter().GetResult();
        var resp = query?.PerformQuery(container.GetItemLinqQueryable<TModel>()) ?? container.GetItemLinqQueryable<TModel>();
        return resp;
    }

    protected virtual async ValueTask<SuccessResult<TModel>> UpdateModel(TKey key, IReadOnlyList<PatchOperation> operations)
    {
        var cont = await GetContainer();
        var result = await cont.PatchItemAsync<TModel>(KeyToString(key), ContainerPartitionKey, operations);
        Tracker?.Add(result.RequestCharge);
        return (int)result.StatusCode is >= 200 and <= 299 ? new(result.Resource) : ModelFailureResult(result.StatusCode);
    }

    public async ValueTask<SuccessResult> Delete(TModel entity)
    {
        var cont = await GetContainer();
        var result = await cont.DeleteItemAsync<TModel>(KeyToString(entity.Id), ContainerPartitionKey);
        return (int)result.StatusCode is >= 200 and <= 299 ? new() : FailureResult(result.StatusCode);
    }

    public async ValueTask<SuccessResult?> Delete(TKey id)
    {
        var cont = await GetContainer();
        var result = await cont.DeleteItemAsync<TModel>(KeyToString(id), ContainerPartitionKey);
        return (int)result.StatusCode is >= 200 and <= 299 ? new() : FailureResult(result.StatusCode);
    }

    public ValueTask<SuccessResult<int>> Delete(IEnumerable<TKey> ids)
    {
        throw new NotSupportedException();
    }

    public ValueTask<SuccessResult<int>> Delete(IQueryable<TModel> entities)
    {
        throw new NotSupportedException();
    }

    protected virtual async ValueTask<SuccessResult<TModel>> Add(TModel model)
    {
        var cont = await GetContainer();
        var result = await cont.CreateItemAsync(model, ContainerPartitionKey);
        return (int)result.StatusCode is >= 200 and <= 299 ? new() : ModelFailureResult(result.StatusCode);
    }

    public abstract ValueTask<SuccessResult<TModel>> Create(TCreateModel creationModel);

    public ValueTask<SuccessResult> SaveChanges()
        => ValueTask.FromResult(SuccessResult.Success);

    public abstract IQueryable<TView> GetViews(IQueryable<TModel> entities);
    public abstract IQueryable<TView> GetViewQueryable(TModel model);
    public abstract TView GetView(TModel model);
    public abstract ValueTask<SuccessResult<TView>?> Update(TKey key, TUpdateModel updateModel);
}
