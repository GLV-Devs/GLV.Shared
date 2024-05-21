using GLV.Shared.Data;
using GLV.Shared.EntityFramework;
using Microsoft.EntityFrameworkCore;

namespace GLV.Shared.Server.API;
public static class GLVEnumerableExtensions
{
    public static IQueryable<TEntity> PerformQuery<TEntity, TKey>(this IQueryable<TEntity> queryable, IEntityQuery<TEntity, TKey>? query)
        where TEntity : class, IDbModel<TEntity, TKey>
        where TKey : unmanaged
        => query is null ? queryable : query.PerformQuery(queryable);

    public static async Task<HashSet<TSource>> ToHashSetAsync<TSource>(
        this IQueryable<TSource> source,
        IEqualityComparer<TSource>? comparer = null,
        CancellationToken cancellationToken = default)
    {
        var list = new HashSet<TSource>(comparer);
        await foreach (var element in source.AsAsyncEnumerable().WithCancellation(cancellationToken))
            list.Add(element);

        return list;
    }
}
