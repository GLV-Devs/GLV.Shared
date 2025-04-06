using Dapper;
using GLV.Shared.ChatBot.EntityFramework.TypeHandlers;
using GLV.Shared.EntityFramework;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Data;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using static GLV.Shared.ChatBot.IConversationStore;

namespace GLV.Shared.ChatBot.EntityFramework;

public class EntityFrameworkConversationStore<TContextModel, TContextModelKey>(
    DbContext context, 
    IConversationStoreCache? cache,
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

    static EntityFrameworkConversationStore()
    {
        SqlMapper.AddTypeHandler(new SqlNullableGuidTypeHandler());
    }

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

    public async ValueTask<FetchConversationResult> FetchConversation(Guid conversationId, bool skipCache = false)
    {
        var sem = await KeyChain.WaitForSemaphoreWithTimeout(conversationId, 500);
        if (sem is null)
            return new FetchConversationResult(null, ConversationContextStatus.ConversationUnderThreadContention);

        try
        {
            if (skipCache is false && Cache is IConversationStoreCache cache && cache.TryGetConversationContext(conversationId, out var convo))
                return new(convo, ConversationContextStatus.ConversationWasObtained);

            TContextModel? cc = null;

            for(int i = 0; i < 3; i++)
                try
                {
                    cc = await context.Database
                                      .GetDbConnection()
                                      .QueryFirstOrDefaultAsync<TContextModel>(
                                          $"select * from {GetContextModelTableName()} where ConversationId = '{conversationId}'",
                                          commandTimeout: 120
                                      );

                    break;
                }
                catch(Exception e)
                {
                    if (e.Message.Contains("timeout", StringComparison.OrdinalIgnoreCase) is false)
                        throw;
                }

            if (cc == null)
                throw new InvalidOperationException("The database timed out too many times");

            //await context.Set<TContextModel>().FirstOrDefaultAsync(x => x.ConversationId == conversationId);
            if (cc is null)
                return new FetchConversationResult(null, ConversationContextStatus.ConversationNotFound);
            else
            {
                var entry = context.Entry(cc);
                Debug.Assert(entry is not null);
                var unpacked = cc?.Unpack();
                return unpacked is not null
                    ? new FetchConversationResult(unpacked, ConversationContextStatus.ConversationWasObtained)
                    : new FetchConversationResult(unpacked, ConversationContextStatus.ConversationCorrupted);
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
            Cache?.RemoveConversationContext(conversationId);

            var connection = context.Database.GetDbConnection();

            using var trans = await connection.BeginTransactionAsync();
            var rows = await connection.ExecuteAsync(
                $"delete from {GetContextModelTableName()} where ConversationId = '{conversationId}'", 
                commandTimeout: 120
            );

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
            Cache?.InsertConversationContext(convo);
            var convoId = convo.ConversationId;
            await UpdateHandler.Invoke(convo, convoId, context, EntityFactory);
        }
        finally
        {
            sem.Release();
        }
    }

    public IConversationStoreCache? Cache { get; } = cache;
}

public sealed class EntityFrameworkConversationStore(DbContext context, IConversationStoreCache? cache = null) 
    : EntityFrameworkConversationStore<ConversationContextPacked, long>(context, cache, ConversationContextPacked.Pack, ConversationContextPacked.UpdateHandler);
