using GLV.Shared.Data;
using GLV.Shared.EntityFramework;
using GLV.Shared.Server.Data;
using Microsoft.EntityFrameworkCore;

namespace GLV.Shared.Server.API;
public static class GLVEnumerableExtensions
{
    public static IQueryable<TEntity> PerformQuery<TEntity, TKey>(this IQueryable<TEntity> queryable, IEntityQuery<TEntity, TKey>? query)
        where TEntity : class, IDbModel<TEntity, TKey>
        where TKey : unmanaged
        => query is null ? queryable : query.PerformQuery(queryable);
}
