using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace GLV.Shared.ChatBot;

public interface IConversationStoreCache
{
    public bool TryGetConversationContext(Guid conversationId, [NotNullWhen(true)] out ConversationContext? context);
    public bool TryPeekConversationContext(Guid conversationId, [NotNullWhen(true)] out ConversationContext? context);
    public void RemoveConversationContext(Guid conversationId);
    public bool InsertConversationContext(ConversationContext context);
}

public class ConversationStoreCache(TimeSpan timeout) : IConversationStoreCache
{
    private readonly record struct ConversationContextEntry(ConversationContext Context, DateTime LastAccesed)
    {
        public ConversationContextEntry GetRefreshed() => new(Context, DateTime.Now);
    }

    public TimeSpan Timeout { get; } 
        = timeout.Ticks > 0 ? timeout : throw new ArgumentException("The cache timeout must be greater than 0", nameof(timeout));

    private readonly Dictionary<Guid, ConversationContextEntry> _d = [];

    public void RunCacheCleanup()
    {
        foreach (var (k, v) in _d)
            if (v.LastAccesed + Timeout < DateTime.Now)
                _d.Remove(k);
    }

    public bool TryGetConversationContext(Guid conversationId, [NotNullWhen(true)] out ConversationContext? context)
    {
        if (_d.TryGetValue(conversationId, out var entry))
        {
            if (entry.LastAccesed + Timeout > DateTime.Now)
            {
                context = entry.Context;
                _d[conversationId] = entry.GetRefreshed();
                return true;
            }

            _d.Remove(conversationId);
        }

        context = null;
        return false;
    }

    public bool TryPeekConversationContext(Guid conversationId, [NotNullWhen(true)] out ConversationContext? context)
    {
        if (_d.TryGetValue(conversationId, out var entry))
        {
            if (entry.LastAccesed + Timeout > DateTime.Now)
            {
                context = entry.Context;
                return true;
            }

            _d.Remove(conversationId);
        }

        context = null;
        return false;
    }

    public void RemoveConversationContext(Guid conversationId)
    {
        _d.Remove(conversationId);
    }

    public bool InsertConversationContext(ConversationContext context)
        => _d.TryAdd(context.ConversationId, new(context, DateTime.Now));
}
