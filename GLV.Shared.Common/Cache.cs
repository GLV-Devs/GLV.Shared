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
    private readonly Func<TKey, CacheEntry, ValueTask<bool>>? IsItemValidChecker;

    public sealed class CacheEntry(TKey key, TCachedItem? item, object? userData)
    {
        public readonly struct CreationInfo(DateTime? storedAt)
        {
            public DateTime StoredAt { get; } = storedAt ?? DateTime.Now;
            public TimeSpan Age => DateTime.Now - StoredAt;
        }

        public TKey Key { get; internal set; } = key;
        public TCachedItem? Item { get; internal set; } = item;
        public object? UserData { get; set; } = userData;
        public CreationInfo Info { get; internal set; } = new();

        internal bool Invalidated;

        internal async ValueTask<bool> CheckValidity(Func<TKey, CacheEntry, ValueTask<bool>>? isItemValidChecker)
        {
            if (Invalidated is false && isItemValidChecker is not null)
                Invalidated = !await isItemValidChecker.Invoke(Key, this);

            return !Invalidated;
        }

        public void Invalidate() => Invalidated = true;
    }

    public Cache(Func<TKey, CacheEntry, ValueTask<bool>>? isItemValidChecker = null, IEqualityComparer<TKey>? equalityComparer = null)
    {
        IsItemValidChecker = isItemValidChecker;
        cachedItems = new(equalityComparer ?? EqualityComparer<TKey>.Default);

        if (IsItemValidChecker is not null)
            BackgroundTaskStore.Add(Task.Run(RunCleanup));
    }

    private readonly Dictionary<TKey, CacheEntry> cachedItems;

    private async Task RunCleanup()
    {
        Debug.Assert(IsItemValidChecker is not null);
        while (true)
        {
            foreach (var (key, item) in cachedItems)
            {
                if (await item.CheckValidity(IsItemValidChecker))
                    continue;

                cachedItems.Remove(key);
            }

            await Task.Delay(1000);
        }
    }

    public async ValueTask<NullableSuccess<CacheEntry?>> TryGetEntry(TKey key)
        => cachedItems.TryGetValue(key, out var item) && await item.CheckValidity(IsItemValidChecker) ? item : NullableSuccess<CacheEntry?>.Failure;

    public async ValueTask<NullableSuccess<TCachedItem?>> TryGetItem(TKey key)
        => cachedItems.TryGetValue(key, out var item) && await item.CheckValidity(IsItemValidChecker) ? item.Item : NullableSuccess<TCachedItem?>.Failure;

    public bool InsertItem(TKey key, TCachedItem? item, object? userData = null)
        => cachedItems.TryAdd(key, new(key, item, userData));

    public bool RemoveItem(TKey key)
        => cachedItems.Remove(key, out var entry);

    public bool RemoveItem(TKey key, out TCachedItem? item)
    {
        var val = cachedItems.Remove(key, out var entry);
        item = entry is null ? default : entry.Item;
        return val;
    }

    public bool RemoveItem(TKey key, out TCachedItem? item, out object? userData)
    {
        var val = cachedItems.Remove(key, out var entry);
        item = entry is null ? default : entry.Item;
        userData = entry is null ? default : entry.UserData;
        return val;
    }
}
