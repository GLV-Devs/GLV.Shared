using Microsoft.EntityFrameworkCore;

namespace GLV.Shared.ChatBot.EntityFramework;

public interface IConversationContextModel<TKey>
{
    public TKey Id { get; set; }
    public Guid ConversationId { get; set; }
    public long Step { get; set; }
    public string? ActiveAction { get; set; }
    public ConversationContext? Unpack();
    public void Update(ConversationContext context);

    public static async Task PerformUpdateQueryThroughEntityFramework<TContextModel>(
        ConversationContext context, 
        Guid conversationId, 
        DbContext db,
        Func<ConversationContext, TContextModel> entityFactory
    ) where TContextModel : class, IConversationContextModel<TKey>
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(db);
        ArgumentNullException.ThrowIfNull(entityFactory);

        var ccp = await db.Set<TContextModel>().FirstOrDefaultAsync(x => x.ConversationId == conversationId);
        if (ccp is null)
            db.Set<TContextModel>().Add(entityFactory.Invoke(context));
        else
            ccp.Update(context);

        await db.SaveChangesAsync();
    }
}
