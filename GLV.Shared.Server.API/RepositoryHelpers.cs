using System.Linq.Expressions;
using GLV.Shared.Data;
using GLV.Shared.Server.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GLV.Shared.Server.API;

public static class RepositoryHelpers
{
    private static ErrorList EntityNotFound(string entityName, string? query = null)
    {
        var errors = new ErrorList();
        return errors.AddEntityNotFound(entityName, query);
    }

    public static async Task<SuccessResult<IAsyncEnumerable<TView>>> QueryEntities<TModel, TKey, TView, TCreateModel, TUpdateModel>(
        this IRepository<TModel, TKey, TView, TCreateModel, TUpdateModel> repository,
        IEntityQuery<TModel, TKey>? query = null
    )
    where TModel : class, IKeyed<TModel, TKey>
    where TKey : unmanaged
    {
        var ents = repository.Get(query);
        return ents is null ? EntityNotFound(nameof(TModel)) : new SuccessResult<IAsyncEnumerable<TView>>(repository.GetViews(ents).AsAsyncEnumerable());
    }

    public static async Task<SuccessResult<IAsyncEnumerable<TView>>> QueryEntities<TModel, TKey, TView, TCreateModel, TUpdateModel>(
        this IRepository<TModel, TKey, TView, TCreateModel, TUpdateModel> repository,
        Expression<Func<TModel, bool>> where,
        IEntityQuery<TModel, TKey>? query = null
    )
    where TModel : class, IKeyed<TModel, TKey>
    where TKey : unmanaged
    {
        var ents = repository.Get(query).Where(where);
        return ents is null ? EntityNotFound(nameof(TModel)) : new SuccessResult<IAsyncEnumerable<TView>>(repository.GetViews(ents).AsAsyncEnumerable());
    }

    public static async Task<SuccessResult<TView>> ViewEntity<TModel, TKey, TView, TCreateModel, TUpdateModel>(
        this IRepository<TModel, TKey, TView, TCreateModel, TUpdateModel> repository,
        TKey key
    )
    where TModel : class, IKeyed<TModel, TKey>
    where TKey : unmanaged
    {
        var findResult = await repository.Find(key);
        return findResult.TryGetResult(out var foundEntity) is false ? findResult.ErrorMessages : repository.GetView(foundEntity);
    }

    public static async Task<SuccessResult<TView>> CreateEntity<TModel, TKey, TView, TCreateModel, TUpdateModel>(
        this IRepository<TModel, TKey, TView, TCreateModel, TUpdateModel> repository,
        [FromBody] TCreateModel creationModel
    )
    where TModel : class, IKeyed<TModel, TKey>
    where TKey : unmanaged
    {
        var result = await repository.Create(creationModel);

        if (result.TryGetResult(out var created))
        {
            var saveResult = await repository.SaveChanges();
            return saveResult.IsSuccess ? await repository.GetViewQueryable(created).FirstAsync() : saveResult.ErrorMessages;
        }

        return result.ErrorMessages;
    }

    public static async Task<SuccessResult> DeleteEntity<TModel, TKey, TView, TCreateModel, TUpdateModel>(
        this IRepository<TModel, TKey, TView, TCreateModel, TUpdateModel> repository,
        TKey key
    )
    where TModel : class, IKeyed<TModel, TKey>
    where TKey : unmanaged
    {
        var r = await repository.Delete(key);

        if (r is not SuccessResult result)
            return EntityNotFound(nameof(TModel), key.ToString());

        else if (result.IsSuccess)
        {
            var saveResult = await repository.SaveChanges();
            return saveResult;
        }
        else
            return result;
    }

    public static async Task<SuccessResult<TView>> UpdateEntity<TModel, TKey, TView, TCreateModel, TUpdateModel>(
        this IRepository<TModel, TKey, TView, TCreateModel, TUpdateModel> repository,
        [FromBody] TUpdateModel update,
        TKey key
    )
    where TModel : class, IKeyed<TModel, TKey>
    where TKey : unmanaged
    {
        var r = await repository.Update(key, update);

        if (r is not SuccessResult<TView> result)
            return EntityNotFound(nameof(TModel), key.ToString());

        if (result.TryGetResult(out var view))
        {
            var saveResult = await repository.SaveChanges();
            return saveResult.IsSuccess ? view : saveResult.ErrorMessages;
        }

        return result;
    }
}
