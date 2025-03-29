using Discord;

namespace GLV.Shared.ChatBot.Discord.DiscordUpdates;

public class DiscordUserCommandUpdateContext : DiscordInteractionUpdateContext
{
    public IUserCommandInteraction UserInteraction { get; }

    internal DiscordUserCommandUpdateContext(
        IChatBotClient client,
        IUserCommandInteraction interaction,
        Guid conversationId,
        bool isHandledByBotClient = false
    ) : base(client, interaction, DiscordUpdateKind.Command, conversationId, isHandledByBotClient)
    {
        UserInteraction = interaction;
        msg = new(interaction.Data.Name, (long)interaction.Id, null, interaction.User?.GetUserInfo(), false);
    }
}
