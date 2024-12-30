using System.Collections.Concurrent;

namespace GLV.Shared.ChatBot;

/// <summary>
/// Maintains a set of <see cref="SemaphoreSlim"/>(1, 1) so that if a bot receives another message from the same conversation while it's still processing the other one, it waits until the first message is processed.
/// </summary>
/// <remarks>
/// Only use this within an implementation of <see cref="IConversationStore"/>
/// </remarks>
public sealed class ConversationStoreKeyChain
{
    private readonly ConcurrentDictionary<Guid, WeakReference<SemaphoreSlim>> SemaphoreDictionary = new();

    /// <summary>
    /// If this method returns null; the semaphore was not obtained. This method automatically waits for the semaphore; all you have to do is release it after you're done
    /// </summary>
    public async ValueTask<SemaphoreSlim?> WaitForSemaphoreWithTimeout(Guid conversationId, int millisecondsTimeout = 500)
    {
        var semwr = SemaphoreDictionary.GetOrAdd(conversationId, k => new WeakReference<SemaphoreSlim>(new SemaphoreSlim(1, 1)));
        SemaphoreSlim? sem;
        lock (semwr)
        {
            if (semwr.TryGetTarget(out sem) is false)
                semwr.SetTarget(sem = new SemaphoreSlim(1, 1));
        }

        if (sem.Wait(100) is false)
            if (await sem.WaitAsync(millisecondsTimeout, default) is false)
                return null;
        return sem;
    }

    /// <summary>
    /// If this method returns null; the semaphore was not obtained. This method automatically waits for the semaphore; all you have to do is release it after you're done
    /// </summary>
    public async ValueTask<SemaphoreSlim> WaitForSemaphore(Guid conversationId)
    {
        var semwr = SemaphoreDictionary.GetOrAdd(conversationId, k => new WeakReference<SemaphoreSlim>(new SemaphoreSlim(1, 1)));
        SemaphoreSlim? sem;
        lock (semwr)
        {
            if (semwr.TryGetTarget(out sem) is false)
                semwr.SetTarget(sem = new SemaphoreSlim(1, 1));
        }

        if (sem.Wait(100) is false)
            await sem.WaitAsync();
        return sem;
    }
}
