using System.Collections;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace GLV.Shared.Hosting;

public sealed class WorkQueue<TItem> : IEnumerable<WorkQueue<TItem>.WorkQueueEntry> 
    where TItem : IWorkQueueItem
{
    private readonly ConcurrentQueue<WorkQueueEntry> _q = new();
    private readonly ConcurrentDictionary<long, WorkQueueEntry> _d = new();

    public bool IsEmpty => _d.IsEmpty;

    public int Count => _d.Count;

    public bool HasPending => _q.IsEmpty;

    public int PendingCount => _q.Count;

    public bool Enqueue(TItem item, [NotNullWhen(true)] out WorkQueueEntry? entry)
    {
        entry = new WorkQueueEntry(item);
        if (_d.TryAdd(item.Id, entry))
        {
            _q.Enqueue(entry);
            return true;
        }

        return false;
    }

    public bool Enqueue(TItem item)
    {
        var entry = new WorkQueueEntry(item);
        if (_d.TryAdd(item.Id, entry))
        {
            _q.Enqueue(entry);
            return true;
        }

        return false;
    }

    public bool ContainsKey(long key)
        => _d.ContainsKey(key);

    /// <summary>
    /// Try to get the value for the given key. This will attempt to remove the entry from the collection if it is complete.
    /// </summary>
    public bool TryGetValue(long key, [MaybeNullWhen(false)] out WorkQueueEntry value, bool removeIfComplete = true)
    {
        if (_d.TryGetValue(key, out value))
        {
            if (removeIfComplete && value.IsCompleted)
                _d.Remove(key, out _);
            return true;
        }

        return false;
    }

    public bool TryDequeue([MaybeNullWhen(false)] out WorkQueueEntry result)
        => _q.TryDequeue(out result);

    public bool TryPeek([MaybeNullWhen(false)] out WorkQueueEntry result) 
        => _q.TryPeek(out result);

    public void Clear()
    {
        _d.Clear();
        _q.Clear();
    }

    public IEnumerator<WorkQueueEntry> GetEnumerator() => _d.Values.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public record class WorkQueueEntry(TItem Item)
    {
        public readonly record struct EntryView(WorkQueueEntryStatus Status, object? Item)
        {
            public string StatusString => Status.ToString();
        }

        public WorkQueueEntryStatus Status { get; set; }
        public bool HasStarted => Status != WorkQueueEntryStatus.NotStarted;
        public bool IsCompleted => Status is WorkQueueEntryStatus.Completed or WorkQueueEntryStatus.Failure or WorkQueueEntryStatus.ExceptionThrown;
        public bool IsCompletedSuccessfully => Status == WorkQueueEntryStatus.Completed;

        public EntryView View => new(Status, Item.View);
    }
}
