using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace GLV.Shared.ChatBot;

public interface IScopedChatBotClient : IChatBotClient
{
    public Guid ScopedConversation { get; }

    public Task<long> RespondWithText(string text)
        => RespondWithText(ScopedConversation, text);

    public Task<long> RespondWithKeyboard(Keyboard keyboard, string? text)
        => RespondWithKeyboard(ScopedConversation, keyboard, text);

    public Task AnswerKeyboardResponse(KeyboardResponse keyboardResponse, string? alertMessage, bool showAlert = false, int cacheTime = 0)
        => AnswerKeyboardResponse(ScopedConversation, keyboardResponse, alertMessage, showAlert, cacheTime);

    public Task DeleteMessage(long messageId)
        => DeleteMessage(ScopedConversation, messageId);

    public Task EditKeyboard(long messageId, Keyboard newKeyboard, string? newText)
        => EditKeyboard(ScopedConversation, messageId, newKeyboard, newText);

    public Task EditText(long messageId, string newText)
        => EditText(ScopedConversation, messageId, newText);
}

public interface IChatBotClient
{
    public string BotId { get; }
    public object UnderlyingBotClientObject { get; }

    public bool IsValidBotCommand(string text, [NotNullWhen(true)] out string? commandName);
    public Task SetBotCommands(IEnumerable<ConversationActionDefinition> commands);
    public Task SetBotDescription(string name, string? shortDescription = null, string? description = null, CultureInfo? culture = null);
    public Task<long> RespondWithText(Guid conversationId, string text);
    public Task<long> RespondWithKeyboard(Guid conversationId, Keyboard keyboard, string? text);
    public Task AnswerKeyboardResponse(Guid conversationId, KeyboardResponse keyboardResponse, string? alertMessage, bool showAlert = false, int cacheTime = 0);
    public Task EditKeyboard(Guid conversationId, long messageId, Keyboard newKeyboard, string? newText);
    public Task EditText(Guid conversationId, long messageId, string newText);
    public Task DeleteMessage(Guid conversationId, long messageId);
    public Task PrepareBot();
}
