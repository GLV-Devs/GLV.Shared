using Discord;

namespace GLV.Shared.ChatBot.Discord.DiscordUpdates;

public class DiscordMessageReceivedUpdateContext : DiscordUpdateContext
{
    public IMessage MessageObject { get; }

    public override Message? Message
        => new(MessageObject.Content, (long)MessageObject.Id, MessageObject.Attachments.Count > 0);

    internal DiscordMessageReceivedUpdateContext(
        IChatBotClient client,
        IMessage messageObject,
        Guid conversationId,
        bool isHandledByBotClient = false
    ) : base(client, conversationId, DiscordUpdateKind.MessageReceived, isHandledByBotClient)
    {
        MessageObject = messageObject ?? throw new ArgumentNullException(nameof(messageObject));
    }
}
