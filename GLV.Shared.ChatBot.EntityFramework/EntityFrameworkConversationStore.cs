using GLV.Shared.EntityFramework;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Runtime.CompilerServices;
using static GLV.Shared.ChatBot.IConversationStore;

namespace GLV.Shared.ChatBot.EntityFramework;

public class EntityFrameworkConversationStore<TContextModel, TContextModelKey>(DbContext context, Func<ConversationContext, TContextModel> entityFactory) : IConversationStore
    where TContextModel : class, IConversationContextModel<TContextModelKey>, IDbModel<TContextModel, TContextModelKey>
    where TContextModelKey : unmanaged
{
    private readonly ConversationStoreKeyChain KeyChain = new();

    public Func<ConversationContext, TContextModel> EntityFactory { get; } = entityFactory ?? throw new ArgumentNullException(nameof(entityFactory));

    public async ValueTask<FetchConversationResult> FetchConversation(Guid conversationId)
    {
        var sem = await KeyChain.WaitForSemaphoreWithTimeout(conversationId, 500);
        if (sem is null)
            return new FetchConversationResult(null, ConversationNotObtainedReason.ConversationUnderThreadContention);

        try
        {
            var cc = await context.Set<TContextModel>().FirstOrDefaultAsync(x => x.ConversationId == conversationId);
            return cc is null
                ? new FetchConversationResult(null, ConversationNotObtainedReason.ConversationNotFound)
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
            var ccp = await context.Set<TContextModel>().FirstOrDefaultAsync(x => x.ConversationId == conversationId);
            if (ccp is not null)
                context.Set<TContextModel>().Remove(ccp);
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
            var convoId = convo.ConversationId;
            var ccp = await context.Set<TContextModel>().FirstOrDefaultAsync(x => x.ConversationId == convoId);
            if (ccp is null)
                context.Set<TContextModel>().Add(EntityFactory.Invoke(convo));
            else
                ccp.Update(convo);

            await context.SaveChangesAsync();
        }
        finally
        {
            sem.Release();
        }
    }
}

public sealed class EntityFrameworkConversationStore(DbContext context) 
    : EntityFrameworkConversationStore<ConversationContextPacked, long>(context, ConversationContextPacked.Pack);
