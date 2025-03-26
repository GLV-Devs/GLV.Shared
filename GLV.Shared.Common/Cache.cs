using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GLV.Shared.Common;

public class Cache<TKey, TCachedItem> where TKey : notnull
{
    private readonly Func<TKey, TCachedItem?, ValueTask<bool>>? IsItemValidChecker;

    public Cache(Func<TKey, TCachedItem?, ValueTask<bool>>? isItemValidChecker = null, IEqualityComparer<TKey>? equalityComparer = null)
    {
        IsItemValidChecker = isItemValidChecker;
        cachedItems = new(equalityComparer ?? EqualityComparer<TKey>.Default);

        if (IsItemValidChecker is not null)
            BackgroundTaskStore.Add(Task.Run(RunCleanup));
    }

    private readonly Dictionary<TKey, TCachedItem?> cachedItems;

    private async Task RunCleanup()
    {
        Debug.Assert(IsItemValidChecker is not null);
        while (true)
        {
            foreach (var (key, item) in cachedItems)
            {
                if (await IsItemValidChecker.Invoke(key, item)) continue;
                cachedItems.Remove(key);
            }

            await Task.Delay(1000);
        }
    }

    public async ValueTask<Success<TCachedItem?>> TryGetItem(TKey key) 
        => IsItemValidChecker is not null
            ? cachedItems.TryGetValue(key, out TCachedItem? item) && await IsItemValidChecker(key, item) ? item : Success<TCachedItem?>.Failure
            : cachedItems.TryGetValue(key, out item) ? item : Success<TCachedItem?>.Failure;

    public bool InsertItem(TKey key, TCachedItem? item)
        => cachedItems.TryAdd(key, item);

    public bool RemoveItem(TKey key, out TCachedItem? item)
        => cachedItems.Remove(key, out item);
}
