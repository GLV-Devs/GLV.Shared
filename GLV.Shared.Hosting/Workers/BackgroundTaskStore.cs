using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace GLV.Shared.Hosting.Workers;

public static class BackgroundTaskStore
{
    private static readonly HashSet<Task> _tasks = [];
    private static readonly ConcurrentQueue<(Func<CancellationToken, Task> Func, DateTime Target)> _funcs = new();
    private static bool active = true;

    static BackgroundTaskStore()
    {
        AppDomain.CurrentDomain.ProcessExit += (s, e) => active = false;
    }

    public static bool Add(Task task)
    {
        if (active is false) return false;
        lock (_tasks)
            _tasks.Add(task);
        return true;
    }

    /// <summary>
    /// Adds a new background task to the store
    /// </summary>
    /// <param name="task">The task to add</param>
    /// <param name="onCompletion">An action to execute when the task completes, whether due to an error or not.</param>
    /// <param name="delay">The amount of time to wait before executing the task</param>
    public static bool Add(Func<CancellationToken, Task> task, TimeSpan? delay = null)
    {
        if (active is false) return false;
        _funcs.Enqueue((task, delay is TimeSpan d ? DateTime.Now + d : DateTime.Now));
        return true;
    }

    /// <summary>
    /// Performs a single sweep on the store, searching for completed tasks to await
    /// </summary>
    public static async Task Sweep(ILogger? log, CancellationToken ct = default)
    {
        List<Exception>? exceptions = null;

        if (_funcs.IsEmpty is false)
        {
            while (_funcs.TryDequeue(out var ft))
            {
                var (func, target) = ft;
                if (DateTime.Now > target)
                    _tasks.Add(func(ct));
                else
                    _funcs.Enqueue(ft);
            }
        }

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
                    lock (_tasks)
                        _tasks.Remove(task);
                }
        }

        if (exceptions?.Count is > 0)
            throw new AggregateException(exceptions);
    }
}
