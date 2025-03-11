using Discord.Commands;
using System.Text.Json.Serialization;

namespace GLV.Shared.ChatBot.Discord;

public enum DiscordUpdateKind
{
    Unknown = 0,
    CommandUpdate = 1
}

public class DiscordCommandUpdateContext(
    IChatBotClient client,
    ICommandContext commandContext,
    Guid conversationId,
    bool isHandledByBotClient = false
) : DiscordUpdateContext(client, DiscordUpdateKind.CommandUpdate, conversationId, isHandledByBotClient)
{
    public ICommandContext CommandContext { get; } = commandContext ?? throw new ArgumentNullException(nameof(commandContext));
    public override Message? Message
        => new(CommandContext.Message.Content, (long)CommandContext.Message.Id, CommandContext.Message.Attachments.Count > 0);
}

public abstract class DiscordUpdateContext(
    IChatBotClient client,
    DiscordUpdateKind updateKind,
    Guid conversationId,
    bool isHandledByBotClient = false
    ) : UpdateContext(client, conversationId, DiscordPlatform)
{
    public DiscordUpdateKind UpdateKind { get; } = updateKind;

    public override bool IsHandledByBotClient => isHandledByBotClient;

    public override KeyboardResponse? KeyboardResponse => null;
}
