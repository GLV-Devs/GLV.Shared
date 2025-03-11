using Dapper;
using GLV.Shared.EntityFramework;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using static GLV.Shared.ChatBot.IConversationStore;

namespace GLV.Shared.ChatBot.EntityFramework;

public class EntityFrameworkConversationStore<TContextModel, TContextModelKey>(
    DbContext context, 
    Func<ConversationContext, TContextModel> entityFactory,
    EntityFrameworkConversationStore<TContextModel, TContextModelKey>.ConversationContextModelUpdateHandler? handler = null
) : IConversationStore
    where TContextModel : class, IConversationContextModel<TContextModelKey>, IDbModel<TContextModel, TContextModelKey>
    where TContextModelKey : unmanaged
{
    public delegate Task ConversationContextModelUpdateHandler(
        ConversationContext context,
        Guid conversationId,
        DbContext db,
        Func<ConversationContext, TContextModel> entityFactory
    );

    private readonly ConversationStoreKeyChain KeyChain = new();

    public bool AllowDeletesToAffectMultipleRows { get; init; } = false;

    public ConversationContextModelUpdateHandler UpdateHandler { get; } = handler
        ?? IConversationContextModel<TContextModelKey>.PerformUpdateQueryThroughEntityFramework;

    public Func<ConversationContext, TContextModel> EntityFactory { get; } = entityFactory ?? throw new ArgumentNullException(nameof(entityFactory));

    private string? __tabname;
    private string GetContextModelTableName()
    {
        __tabname ??= context.Set<TContextModel>().EntityType.GetTableName();
        Debug.Assert(string.IsNullOrWhiteSpace(__tabname) is false);
        return __tabname;
    }

    public async ValueTask<FetchConversationResult> FetchConversation(Guid conversationId)
    {
        var sem = await KeyChain.WaitForSemaphoreWithTimeout(conversationId, 500);
        if (sem is null)
            return new FetchConversationResult(null, ConversationNotObtainedReason.ConversationUnderThreadContention);

        try
        {
            var cc = (await context.Database
                                   .GetDbConnection()
                                   .QueryFirstAsync<TContextModel>($"select * from {GetContextModelTableName()} where ConversationId = '{conversationId}'"));

            //await context.Set<TContextModel>().FirstOrDefaultAsync(x => x.ConversationId == conversationId);
            if (cc is null)
                return new FetchConversationResult(null, ConversationNotObtainedReason.ConversationNotFound);
            else
            {
                var entry = context.Entry(cc);
                Debug.Assert(entry is not null);
                return new FetchConversationResult(cc?.Unpack(), ConversationNotObtainedReason.ConversationWasObtained);
            }
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
            var connection = context.Database.GetDbConnection();

            using var trans = await connection.BeginTransactionAsync();
            var rows = await connection.ExecuteAsync($"delete from {GetContextModelTableName()} where ConversationId = '{conversationId}'");

            Debug.Assert(rows >= 0);
            if (AllowDeletesToAffectMultipleRows is false && rows > 1)
                throw new InvalidOperationException
                    ($"When trying to delete conversation {conversationId}, more than one row was affected. Rolling back changes.");

            await trans.CommitAsync();
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
            await UpdateHandler.Invoke(convo, convoId, context, entityFactory);
        }
        finally
        {
            sem.Release();
        }
    }
}

public sealed class EntityFrameworkConversationStore(DbContext context) 
    : EntityFrameworkConversationStore<ConversationContextPacked, long>(context, ConversationContextPacked.Pack, ConversationContextPacked.UpdateHandler);
