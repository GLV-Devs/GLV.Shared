using Discord;

namespace GLV.Shared.ChatBot.Discord.DiscordUpdates;

public class DiscordSlashCommandUpdateContext : DiscordInteractionUpdateContext
{
    public ISlashCommandInteraction SlashInteraction { get; }

    internal DiscordSlashCommandUpdateContext(
        IChatBotClient client,
        ISlashCommandInteraction interaction,
        Guid conversationId,
        bool isHandledByBotClient = false
    ) : base(client, interaction, DiscordUpdateKind.Command, conversationId, isHandledByBotClient)
    {
        SlashInteraction = interaction;
        msg = new(interaction.Data.Name, (long)interaction.Id, null, interaction.User?.GetUserInfo(), false);
    }
}

public class DiscordComponentUpdateContext : DiscordInteractionUpdateContext
{
    public IComponentInteraction ComponentInteraction { get; }

    public override KeyboardResponse? KeyboardResponse { get; }

    internal DiscordComponentUpdateContext(
        IChatBotClient client,
        IComponentInteraction interaction,
        Guid conversationId,
        bool isHandledByBotClient = false
    ) : base(client, interaction, DiscordUpdateKind.Command, conversationId, isHandledByBotClient)
    {
        ComponentInteraction = interaction;
        KeyboardResponse = new(interaction.Id.ToString(), interaction.Data.CustomId)
        {
            AttachedData = ComponentInteraction
        };
    }
}
