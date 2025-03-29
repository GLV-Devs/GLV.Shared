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

        var refMessage = MessageObject.Reference?.MessageId;
        long? refmsid = refMessage is Optional<ulong> rm && rm.IsSpecified ? (long)rm.Value : null;

        Message = new(
            MessageObject.Content,
            (long)MessageObject.Id,
            refmsid,
            MessageObject.Author?.GetUserInfo(),
            MessageObject.Attachments?.Count is > 0
        );

        IsDirectMessage = messageObject.Channel is IDMChannel;
    }
}
