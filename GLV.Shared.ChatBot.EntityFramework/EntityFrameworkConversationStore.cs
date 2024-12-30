using Microsoft.EntityFrameworkCore;
using System.Runtime.CompilerServices;
using static GLV.Shared.ChatBot.IConversationStore;

namespace GLV.Shared.ChatBot.EntityFramework;

public sealed class EntityFrameworkConversationStore(DbContext context) : IConversationStore
{
    private readonly ConversationStoreKeyChain KeyChain = new();

    public async ValueTask<FetchConversationResult> FetchConversation(Guid conversationId)
    {
        var sem = await KeyChain.WaitForSemaphoreWithTimeout(conversationId, 500);
        if (sem is null)
            return new FetchConversationResult(null, ConversationNotObtainedReason.ConversationUnderThreadContention);

        try
        {
            var cc = await context.Set<ConversationContextPacked>().FindAsync(conversationId);
            return cc is null
                ? new FetchConversationResult(null, ConversationNotObtainedReason.ConversationWasObtained)
                : new FetchConversationResult(cc?.Unpack(), ConversationNotObtainedReason.ConversationWasObtained);
        }
        finally
        {
            sem.Release();
        }
    }

    public async Task DeleteConversation(Guid conversationId)
    {
        var sem = await KeyChain.WaitForSemaphore(conversationId);
        try
        {
            var ccp = await context.Set<ConversationContextPacked>().FindAsync(conversationId);
            if (ccp is not null)
                context.Set<ConversationContextPacked>().Remove(ccp);
        }
        finally
        {
            sem.Release();
        }
    }

    public async Task SaveChanges(ConversationContext convo)
    {
        var sem = await KeyChain.WaitForSemaphore(convo.ConversationId);
        try
        {
            var ccp = await context.Set<ConversationContextPacked>().FindAsync(convo.ConversationId);
            if (ccp is null)
                context.Set<ConversationContextPacked>().Add(ConversationContextPacked.Pack(convo));
            else
            {
                var entry = context.Entry(ccp);
                if (entry.State is not EntityState.Deleted)
                {
                    entry.CurrentValues.SetValues(ConversationContextPacked.Pack(convo));
                    entry.State = EntityState.Modified;
                }
            }
            await context.SaveChangesAsync();
        }
        finally
        {
            sem.Release();
        }
    }
}
