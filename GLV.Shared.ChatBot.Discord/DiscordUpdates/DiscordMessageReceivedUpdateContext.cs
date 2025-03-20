using Discord;

namespace GLV.Shared.ChatBot.Discord.DiscordUpdates;

public class DiscordMessageReceivedUpdateContext : DiscordUpdateContext
{
    public IMessage MessageObject { get; }

    public override Message? Message
    {
        get
        {
            var refMessage = MessageObject.Reference.MessageId;
            long? refmsid = refMessage.IsSpecified ? (long)refMessage.Value : null;

            return new(
                MessageObject.Content, 
                (long)MessageObject.Id, 
                refmsid, 
                new UserInfo(
                    MessageObject.Author.Username,
                    MessageObject.Author.GlobalName,
                    MessageObject.Author.PackDiscordUserId()
                ), 
                MessageObject.Attachments.Count > 0
            );
        }
    }

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
