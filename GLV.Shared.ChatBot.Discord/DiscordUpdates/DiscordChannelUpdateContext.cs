using Discord;

namespace GLV.Shared.ChatBot.Discord.DiscordUpdates;

public class DiscordChannelUpdatedUpdateContext : DiscordChannelUpdateContext
{
    internal DiscordChannelUpdatedUpdateContext(
        IChatBotClient client,
        IChannel oldChannel,
        IChannel channel,
        Guid conversationId,
        bool isHandledByBotClient = false
    ) : base(client, channel, conversationId, DiscordUpdateKind.ChannelUpdated, isHandledByBotClient)
    {
        OldChannel = oldChannel;
    }

    public IChannel OldChannel { get; }
}

public class DiscordChannelUpdateContext : DiscordUpdateContext
{
    public override Message? Message => null;
    public IChannel Channel { get; }

    internal DiscordChannelUpdateContext(
        IChatBotClient client,
        IChannel channel,
        Guid conversationId,
        DiscordUpdateKind kind,
        bool isHandledByBotClient = false
    ) : base(client, conversationId, kind, isHandledByBotClient)
    {
        Channel = channel;
    }
}
