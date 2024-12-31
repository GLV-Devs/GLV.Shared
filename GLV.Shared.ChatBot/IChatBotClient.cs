using System.Globalization;

namespace GLV.Shared.ChatBot;

public interface IChatBotClient
{
    public string BotId { get; }
    public object UnderlyingBotClientObject { get; }

    public Task SetBotCommands(IEnumerable<ConversationCommandDefinition> commands);
    public Task SetBotDescription(string name, string? shortDescription = null, string? description = null, CultureInfo? culture = null);
    public Task RespondWithText(Guid conversationId, string text);
}
