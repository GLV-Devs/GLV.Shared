using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace GLV.Shared.Server.API;
public static class EnumerableHelpers
{
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
