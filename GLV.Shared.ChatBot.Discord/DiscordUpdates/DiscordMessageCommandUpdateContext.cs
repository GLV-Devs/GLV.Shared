using Discord;

namespace GLV.Shared.ChatBot.Discord.DiscordUpdates;

public class DiscordMessageCommandUpdateContext : DiscordInteractionUpdateContext
{
    public IMessageCommandInteraction MessageInteraction { get; }

    internal DiscordMessageCommandUpdateContext(
        IChatBotClient client,
        IMessageCommandInteraction interaction,
        Guid conversationId,
        bool isHandledByBotClient = false
    ) : base(client, interaction, DiscordUpdateKind.Command, conversationId, isHandledByBotClient)
    {
        MessageInteraction = interaction;
        msg = new(interaction.Data.Name, (long)interaction.Id, null, interaction.User?.GetUserInfo(), false);
    }
}
