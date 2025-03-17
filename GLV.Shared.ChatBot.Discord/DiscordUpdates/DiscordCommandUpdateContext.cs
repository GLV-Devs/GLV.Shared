using Discord.Commands;

namespace GLV.Shared.ChatBot.Discord.DiscordUpdates;

public class DiscordCommandUpdateContext : DiscordUpdateContext
{
    public CommandInfo CommandInfo { get; }
    public ICommandContext CommandContext { get; }
    public override Message? Message
        => new(
            CommandContext.Message.Content, 
            (long)CommandContext.Message.Id,
            null,
            CommandContext.Message.Author is not null 
                ? new UserInfo(
                    CommandContext.Message.Author.Mention, 
                    CommandContext.Message.Author.Username, 
                    CommandContext.Message.Author.PackDiscordUserId()
                )
                : null,
            CommandContext.Message.Attachments.Count > 0
        );

    internal DiscordCommandUpdateContext(
        IChatBotClient client,
        ICommandContext commandContext,
        CommandInfo commandInfo,
        Guid conversationId,
        bool isHandledByBotClient = false
    ) : base(client, conversationId, DiscordUpdateKind.Command, isHandledByBotClient)
    {
        CommandInfo = commandInfo;
        CommandContext = commandContext ?? throw new ArgumentNullException(nameof(commandContext));
    }
}
