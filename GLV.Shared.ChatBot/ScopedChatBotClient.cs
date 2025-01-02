using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace GLV.Shared.ChatBot;

public class ScopedChatBotClient(Guid scopedConversation, IChatBotClient client) : IScopedChatBotClient
{
    public IChatBotClient Client { get; } = client ?? throw new ArgumentNullException(nameof(client));

    public Guid ScopedConversation { get; } = scopedConversation;

    public string BotId => Client.BotId;

    public object UnderlyingBotClientObject => Client.UnderlyingBotClientObject;

    public bool IsValidBotCommand(string text, [NotNullWhen(true)] out string? commandName) 
        => Client.IsValidBotCommand(text, out commandName);

    public Task SetBotCommands(IEnumerable<ConversationActionDefinition> commands) 
        => Client.SetBotCommands(commands);

    public Task SetBotDescription(string name, string? shortDescription = null, string? description = null, CultureInfo? culture = null) 
        => Client.SetBotDescription(name, shortDescription, description, culture);

    public Task RespondWithText(Guid conversationId, string text) 
        => Client.RespondWithText(conversationId, text);

    public Task RespondWithKeyboard(Guid conversationId, Keyboard keyboard, string? text)
        => Client.RespondWithKeyboard(conversationId, keyboard, text);
}
