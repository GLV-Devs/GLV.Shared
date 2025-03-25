using System.Linq.Expressions;
using GLV.Shared.Data;
using GLV.Shared.Server.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
namespace GLV.Shared.Server.API.Controllers;

public abstract class RepositoryController<TUser, TContext, TModel, TKey, TView, TCreateModel, TUpdateModel>(
    ILogger<RepositoryController<TUser, TContext, TModel, TKey, TView, TCreateModel, TUpdateModel>> logger,
    IRepository<TModel, TKey, TView, TCreateModel, TUpdateModel> repository
) : AppController<TUser>(logger)
    where TContext : DbContext
    where TUser : class
    where TModel : class, IKeyed<TModel, TKey>
    where TKey : unmanaged
{
    protected readonly IRepository<TModel, TKey, TView, TCreateModel, TUpdateModel> Repository = repository ?? throw new ArgumentNullException(nameof(repository));

    [FromServices]
    public TContext Context { get; init; } = null!;

    [NonAction]
    public virtual Task<bool> CheckIfEntityExists<TEntity, TEntityKey>(TEntityKey key)
        where TEntity : class, IKeyed<TEntity, TEntityKey>
        where TEntityKey : unmanaged
            => Context.Set<TEntity>().AnyAsync(x => x.Id.Equals(key));

    [NonAction]
    public virtual ValueTask<TEntity?> FindEntity<TEntity, TEntityKey>(TEntityKey key)
        where TEntity : class, IKeyed<TEntity, TEntityKey>
        where TEntityKey : unmanaged
            => Context.Set<TEntity>().FindAsync(key);

    [NonAction]
    public virtual async Task<IActionResult> QueryEntities(Expression<Func<TModel, bool>> where, IEntityQuery<TModel, TKey>? query = null)
        => FromSuccessResult(await Repository.QueryEntities(where, query));

    [NonAction]
    public virtual async Task<IActionResult> QueryEntities(IEntityQuery<TModel, TKey>? query = null)
        => FromSuccessResult(await Repository.QueryEntities(query));

    [NonAction]
    public virtual async Task<IActionResult> ViewEntity(TKey key)
        => FromSuccessResult(await Repository.ViewEntity(key));

    [NonAction]
    public virtual async Task<IActionResult> CreateEntity([FromBody] TCreateModel creationModel)
        => FromSuccessResult(await Repository.CreateEntity(creationModel));

    [NonAction]
    public virtual async Task<IActionResult> DeleteEntity(TKey key)
        => FromSuccessResult(await Repository.DeleteEntity(key));

    [NonAction]
    public virtual async Task<IActionResult> UpdateEntity([FromBody] TUpdateModel update, TKey key)
        => FromSuccessResult(await Repository.UpdateEntity(update, key));
}
