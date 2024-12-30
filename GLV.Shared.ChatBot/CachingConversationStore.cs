using System.Collections.Concurrent;
using static GLV.Shared.ChatBot.IConversationStore;

namespace GLV.Shared.ChatBot;

public class CachingConversationStore(IConversationStore backingStore) : IConversationStore
{
    private readonly ConversationStoreKeyChain KeyChain = new();

    private readonly record struct ConversationRecord(ConversationContext Context, DateTime Expiration)
    {
        public ConversationRecord Refresh(TimeSpan expiration)
            => new(Context, DateTime.Now + expiration);

        public static ConversationRecord New(ConversationContext context, TimeSpan expiration)
            => new (context, DateTime.Now + expiration);

        public static void RefRefresh(ref ConversationRecord record, TimeSpan expiration)
            => record = new(record.Context, DateTime.Now + expiration);
    }

    private readonly IConversationStore backingStore = backingStore ?? throw new ArgumentNullException(nameof(backingStore));
    private readonly ConcurrentDictionary<Guid, ConversationRecord> cache = [];

    public TimeSpan CacheExpirationTime { get; set; } = TimeSpan.FromMinutes(10);

    public async ValueTask<FetchConversationResult> FetchConversation(Guid conversationId)
    {
        var sem = await KeyChain.WaitForSemaphoreWithTimeout(conversationId, 500);
        if (sem is null)
            return new FetchConversationResult(null, ConversationNotObtainedReason.ConversationUnderThreadContention);
        try
        {
            if (cache.TryGetValue(conversationId, out var convoRecord))
            {
                cache[conversationId] = convoRecord.Refresh(CacheExpirationTime);
                return new(convoRecord.Context, ConversationNotObtainedReason.ConversationWasObtained);
            }

            var cont = await backingStore.FetchConversation(conversationId);
            if (cont.NotObtainedReason is not ConversationNotObtainedReason.ConversationWasObtained) return cont;

            cache[conversationId] = ConversationRecord.New(cont.Context!, CacheExpirationTime);
            return cont;
        }
        finally
        {
            sem.Release();
        }
    }

    public Task DeleteConversation(Guid conversationId)
    {
        cache.Remove(conversationId, out _);
        return backingStore.DeleteConversation(conversationId);
    }

    public Task SaveChanges(ConversationContext context) 
        => backingStore.SaveChanges(context);

    /// <summary>
    /// This method should be called periodically, ideally in a background task through <see cref="PeriodicallyCleanExpiredRecords"/>
    /// </summary>
    public void CleanExpiredRecords()
    {
        foreach (var (key, record) in cache)
            if (record.Expiration > DateTime.Now)
                cache.TryRemove(key, out _);
    }

    public async Task PeriodicallyCleanExpiredRecords(CancellationToken ct = default)
    {
        while (ct.IsCancellationRequested is false)
        {
            CleanExpiredRecords();
            await Task.Delay(1000, default);
        }
    }
}
