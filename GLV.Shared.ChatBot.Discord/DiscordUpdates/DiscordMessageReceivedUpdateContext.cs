using Discord;

namespace GLV.Shared.ChatBot.Discord.DiscordUpdates;

public class DiscordMessageReceivedUpdateContext : DiscordUpdateContext
{
    public IMessage MessageObject { get; }

    public override Message? Message
    {
        get
        {
            var refMessage = MessageObject.Reference?.MessageId;
            long? refmsid = refMessage is Optional<ulong> rm && rm.IsSpecified ? (long)rm.Value : null;

            return new(
                MessageObject.Content, 
                (long)MessageObject.Id, 
                refmsid,
                MessageObject.Author is not null 
                ? new UserInfo(
                    MessageObject.Author.Username,
                    MessageObject.Author.GlobalName,
                    MessageObject.Author.PackDiscordUserId()
                )
                : null, 
                MessageObject.Attachments?.Count is > 0
            );
        }
    }

    public override MemberEvent? MemberEvent => null;

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
