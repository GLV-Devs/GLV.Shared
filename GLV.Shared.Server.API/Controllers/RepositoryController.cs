using System.Linq.Expressions;
using GLV.Shared.Data;
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

    public RepositoryController(
        ILogger<RepositoryController<TUser, TContext, TModel, TKey, TView, TCreateModel, TUpdateModel>> logger,
        IRepository<TModel, TKey, TView, TCreateModel, TUpdateModel> repository,
        UserManager<TUser> userManager
    ) : this(logger, repository) { }

    [FromServices]
    protected TContext Context { get; init; }

    protected virtual IActionResult EntityNotFound(string entityName, string? query = null)
    {
        var errors = new ErrorList();
        errors.AddEntityNotFound(entityName, query);
        return FailureResult(errors);
    }

    protected override IActionResult Forbidden(object? value)
        => value is null ? Forbidden() : base.Forbidden(value);

    protected virtual IActionResult Forbidden()
    {
        var errors = new ErrorList();
        errors.AddNoPermission();
        return FailureResult(errors);
    }

    protected virtual Task<bool> CheckIfEntityExists<TEntity, TEntityKey>(TEntityKey key)
        where TEntity : class, IKeyed<TEntity, TEntityKey>
        where TEntityKey : unmanaged
            => Context.Set<TEntity>().AnyAsync(x => x.Id.Equals(key));

    protected virtual ValueTask<TEntity?> FindEntity<TEntity, TEntityKey>(TEntityKey key)
        where TEntity : class, IKeyed<TEntity, TEntityKey>
        where TEntityKey : unmanaged
            => Context.Set<TEntity>().FindAsync(key);

    protected virtual async Task<IActionResult> QueryEntities(Expression<Func<TModel, bool>> where)
    {
        var ents = Repository.Get().Where(where);
        return ents is null || await ents.AnyAsync() is false ? NotFound() : Ok(Repository.GetViews(ents).AsAsyncEnumerable());
    }

    protected virtual async Task<IActionResult> QueryEntities()
    {
        var ents = Repository.Get();
        return ents is null || await ents.AnyAsync() is false ? NotFound() : Ok(Repository.GetViews(ents).AsAsyncEnumerable());
    }

    protected virtual async Task<IActionResult> ViewEntity(TKey key)
    {
        var findResult = await Repository.Find(key);

        if (findResult.TryGetResult(out var foundEntity) is false)
            return FailureResult(findResult);

        var view = Repository.GetView(foundEntity);
        return Ok(view);
    }

    protected virtual async Task<IActionResult> CreateEntity([FromBody] TCreateModel creationModel)
    {
        var result = await Repository.Create(creationModel);

        if (result.TryGetResult(out var created))
        {
            var saveResult = await Repository.SaveChanges();
            return saveResult.IsSuccess ? Created((string?)null, Repository.GetView(created)) : FailureResult(saveResult);
        }

        return FailureResult(result);
    }

    protected virtual async Task<IActionResult> DeleteEntity(TKey key)
    {
        var r = await Repository.Delete(key);

        if (r is not SuccessResult result)
            return NotFound();

        else if (result.IsSuccess)
        {
            var saveResult = await Repository.SaveChanges();
            return saveResult.IsSuccess ? Ok() : FailureResult(saveResult);
        }
        else
            return FailureResult(result);
    }

    protected virtual async Task<IActionResult> UpdateEntity([FromBody] TUpdateModel update, TKey key)
    {
        var r = await Repository.Update(key, update);

        if (r is not SuccessResult<TView> result)
            return NotFound();

        if (result.TryGetResult(out var view))
        {
            var saveResult = await Repository.SaveChanges();
            return saveResult.IsSuccess ? Ok(view) : FailureResult(saveResult);
        }

        return FailureResult(result);
    }
}
