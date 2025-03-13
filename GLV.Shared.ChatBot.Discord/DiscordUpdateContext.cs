using System.Text.Json.Serialization;

namespace GLV.Shared.ChatBot.Discord;

public abstract class DiscordUpdateContext : UpdateContext
{
    internal DiscordUpdateContext(
        IChatBotClient client,
        Guid conversationId,
        DiscordUpdateKind updateKind,
        bool isHandledByBotClient = false
    ) : base(client, conversationId, DiscordPlatform)
    {
        IsHandledByBotClient = isHandledByBotClient;
        UpdateKind = updateKind;
    }

    public DiscordUpdateKind UpdateKind { get; }

    public override KeyboardResponse? KeyboardResponse => null;
}

public enum DiscordUpdateKind
{
    Command = 0,
    ChannelUpdated = 1,
    ChannelDeleted = 2,
    ChannelCreated = 3,
    MessageReceived = 4
}