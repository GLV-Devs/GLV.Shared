using Discord;

namespace GLV.Shared.ChatBot.Discord.DiscordUpdates;

public class DiscordMessageReceivedUpdateContext : DiscordUpdateContext
{
    public IMessage MessageObject { get; }
    public override bool IsDirectMessage { get; }
    public override Message? Message { get; }
    public override MemberEvent? MemberEvent => null;

    internal DiscordMessageReceivedUpdateContext(
        IChatBotClient client,
        IMessage messageObject,
        Guid conversationId,
        bool isHandledByBotClient = false
    ) : base(client, conversationId, DiscordUpdateKind.MessageReceived, isHandledByBotClient)
    {
        MessageObject = messageObject ?? throw new ArgumentNullException(nameof(messageObject));

        Message = new(
            MessageObject.Content,
            (long)MessageObject.Id,
            MessageObject.Reference?.GetMessageReferenceInfo(this).ConfigureAwait(false).GetAwaiter().GetResult(),
            MessageObject.Author?.GetUserInfo(),
            MessageObject.Attachments?.Count is > 0,
            MessageObject
        );

        IsDirectMessage = messageObject.Channel is IDMChannel;
    }
}
