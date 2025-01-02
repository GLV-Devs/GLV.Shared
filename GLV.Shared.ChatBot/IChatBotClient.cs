using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace GLV.Shared.ChatBot;

public interface IScopedChatBotClient : IChatBotClient
{
    public Guid ScopedConversation { get; }
    public Task RespondWithText(string text)
        => RespondWithText(ScopedConversation, text);

    public Task RespondWithKeyboard(Keyboard keyboard, string? text)
        => RespondWithKeyboard(ScopedConversation, keyboard, text);
}

public interface IChatBotClient
{
    public string BotId { get; }
    public object UnderlyingBotClientObject { get; }

    public bool IsValidBotCommand(string text, [NotNullWhen(true)] out string? commandName);
    public Task SetBotCommands(IEnumerable<ConversationActionDefinition> commands);
    public Task SetBotDescription(string name, string? shortDescription = null, string? description = null, CultureInfo? culture = null);
    public Task RespondWithText(Guid conversationId, string text);
    public Task RespondWithKeyboard(Guid conversationId, Keyboard keyboard, string? text);
}
