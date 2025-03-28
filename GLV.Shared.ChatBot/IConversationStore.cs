using GLV.Shared.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace GLV.Shared.ChatBot;

public interface IConversationStore
{
    public enum ConversationContextStatus
    {
        Unknown = 0,
        ConversationWasObtained,
        ConversationNotFound,
        ConversationUnderThreadContention,
        ConversationObtainedFromCache,
        ConversationCorrupted
    }

    public readonly record struct FetchConversationResult(ConversationContext? Context, ConversationContextStatus Status);

    public IConversationStoreCache? Cache { get; }
    public bool IsCachingEnabled => Cache is not null;

    public ValueTask<FetchConversationResult> FetchConversation(Guid conversationId, bool skipCache = false);
    public Task DeleteConversation(Guid conversationId);
    public Task SaveChanges(ConversationContext context);

    // TODO: Consider making these queryable through an interface, such as IConversationContextFacade. As in IQueryable<IConversationContextFacade>
    // This way, things such as the EntityFramework store can simply implement the interface and still use a different class altogether that packs the actual convo
    // If EntityFramework plays nice, both allowing arbitrary sub-classing of conversation contexts and a rich API that doesn't limit
    // how simple the store can be can be kept
    // As far as storing the contexts as files goes, it would also allow for this. Simply deserialize a smaller class that only contains the data that can be queried.
}
