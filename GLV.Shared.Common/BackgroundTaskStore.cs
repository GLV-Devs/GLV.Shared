using System.Buffers;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace GLV.Shared.Common;

public static class BackgroundTaskStore
{
    private static readonly SemaphoreSlim sem = new(1, 1);
    private static readonly HashSet<Task> _tasks = [];
    private static bool active = true;

    public static void Disable()
        => active = false;

    public static void Enable()
        => active = true;

    static BackgroundTaskStore()
    {
        AppDomain.CurrentDomain.ProcessExit += (s, e) => active = false;
    }

    public static bool Add(Task task)
    {
        if (active is false) return false;
        sem.Wait();
        try
        {
            _tasks.Add(task);
        }
        finally
        {
            sem.Release();
        }
        return true;
    }

    public static async Task WaitForAll(CancellationToken ct = default)
    {
        if (sem.Wait(200, ct) is false)
            await sem.WaitAsync(ct);

        Task whenAll;
        try
        {
            int len = _tasks.Count;
            Span<Task> tasks = ArrayPool<Task>.Shared.Rent(len).AsSpan(0, len);
            int i = 0;
            foreach (var task in _tasks)
                tasks[i++] = task;
            _tasks.Clear();
            whenAll = Task.WhenAll(tasks);
        }
        finally
        {
            sem.Release();
        }
        await whenAll;
    }

    /// <summary>
    /// Performs a single sweep on the store, searching for completed tasks to await
    /// </summary>
    public static async Task Sweep(CancellationToken ct = default)
    {
        List<Exception>? exceptions = null;

        foreach (var task in _tasks)
        {
            ct.ThrowIfCancellationRequested();
            if (task.IsCompleted)
                try
                {
                    await task;
                }
                catch (Exception e)
                {
                    (exceptions ??= []).Add(e);
                }
                finally
                {
                    if (sem.Wait(200, ct) is false)
                        await sem.WaitAsync(ct);
                    try
                    {
                        _tasks.Remove(task);
                    }
                    finally
                    {
                        sem.Release();
                    }
                }
        }

        if (exceptions?.Count is > 0)
            throw new AggregateException(exceptions);
    }
}
