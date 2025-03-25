using System.Diagnostics;
using System.Linq.Expressions;
using System.Net;
using GLV.Shared.Data;
using GLV.Shared.Server.Data;
using Microsoft.EntityFrameworkCore;

namespace GLV.Shared.EntityFramework;

public abstract class EntityFrameworkRepository<TEntity, TKey, TView, TCreateModel, TUpdateModel>(DbContext context)
    : IRepository<TEntity, TKey, TView, TCreateModel, TUpdateModel>
    where TEntity : class, IKeyed<TEntity, TKey>
    where TKey : unmanaged, IEquatable<TKey>
{
    public DbContext Context { get; } = context ?? throw new ArgumentNullException(nameof(context));

    public abstract Expression<Func<TEntity, TView>> ViewExpression { get; }

    protected virtual Expression<Func<TEntity, bool>> Match(TEntity entity)
        => x => x.Id.Equals(entity.Id);

    protected virtual Expression<Func<TEntity, bool>> Match(TKey key)
        => x => x.Id.Equals(key);

    public IQueryable<TView> GetViewQueryable(TEntity model)
        => GetViews(Get().Where(Match(model)));

    public virtual async ValueTask<SuccessResult<TEntity>> Find(TKey id)
    {
        var t = Get();
        if (t is not null)
        {
            var ent = await t.Where(Match(id)).FirstOrDefaultAsync();

            if (ent is not null)
                return new SuccessResult<TEntity>(ent);
        }

        ErrorList errors = new();
        errors.AddError(ErrorMessages.EntityNotFound(typeof(TEntity).Name, $"id: {id}"));
        return new SuccessResult<TEntity>(errors);
    }

    public virtual async ValueTask<SuccessResult> Delete(TEntity entity)
    {
        var entities = Get();
        var check = entities?.ContainsAsync(entity);
        if (check is null || await check is not true)
        {
            ErrorList err = new(HttpStatusCode.Forbidden);
            return new(err.AddEntityNotFound(typeof(TEntity).Name, $"id: {entity.Id}"));
        }

        Debug.Assert(entities is not null);
        await entities.Where(Match(entity)).ExecuteDeleteAsync();

        return SuccessResult.Success;
    }

    public async ValueTask<SuccessResult?> Delete(TKey id)
    {
        var entities = Get();
        var check = entities?.AnyAsync(Match(id));
        if (check is null || await check is not true)
        {
            ErrorList err = new(HttpStatusCode.Forbidden);
            return new(err.AddEntityNotFound(typeof(TEntity).Name, $"id: {id}"));
        }

        Debug.Assert(entities is not null);
        await entities.Where(Match(id)).ExecuteDeleteAsync();

        return SuccessResult.Success;
    }

    public virtual async ValueTask<SuccessResult<int>> Delete(IEnumerable<TKey> ids)
        => await Get().Where(x => ids.Contains(x.Id)).ExecuteDeleteAsync();

    public abstract ValueTask<SuccessResult<TEntity>> Create(TCreateModel creationModel);

    public virtual ValueTask<SuccessResult> SaveChanges()
        => context.TrySaveChanges();

    public virtual IQueryable<TEntity> Get(IEntityQuery<TEntity, TKey>? query = null)
        => query is null ? Context.Set<TEntity>() : query.PerformQuery(Context.Set<TEntity>());

    public IQueryable<TView> GetViews(IQueryable<TEntity> entities)
        => entities.Select(ViewExpression);

    public abstract TView GetView(TEntity entity);

    public abstract ValueTask<SuccessResult<TView>?> Update(TKey key, TUpdateModel updateModel);

    public async ValueTask<SuccessResult<int>> Delete(IQueryable<TEntity> entities)
        => await entities.ExecuteDeleteAsync();
}
