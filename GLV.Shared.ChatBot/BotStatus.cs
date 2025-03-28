namespace GLV.Shared.ChatBot;

public abstract class BotStatus(IChatBotClient client)
{
    protected IChatBotClient Client { get; } = client;
    public abstract Task<IDisposable> SetStatus(Guid conversationId);
}

public readonly record struct ScopedBotStatus(BotStatus? Status, Guid ScopedConversation)
{
    public Task<IDisposable>? SetStatus()
        => Status?.SetStatus(ScopedConversation);
}
