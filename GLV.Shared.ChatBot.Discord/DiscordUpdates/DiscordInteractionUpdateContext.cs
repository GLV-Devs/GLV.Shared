using Discord;

namespace GLV.Shared.ChatBot.Discord.DiscordUpdates;

public class DiscordInteractionUpdateContext : DiscordUpdateContext
{
    protected Message? msg;

    public IDiscordInteraction Interaction { get; }
    public override Message? Message => msg;
    public override MemberEvent? MemberEvent => null;

    internal DiscordInteractionUpdateContext(
        IChatBotClient client,
        IDiscordInteraction interaction,
        DiscordUpdateKind updateKind,
        Guid conversationId,
        bool isHandledByBotClient = false
    ) : base(client, conversationId, updateKind, isHandledByBotClient)
    {
        Interaction = interaction ?? throw new ArgumentNullException(nameof(interaction));
        IsDirectMessage = interaction.IsDMInteraction;
    }

    public override bool IsDirectMessage { get; }
}
