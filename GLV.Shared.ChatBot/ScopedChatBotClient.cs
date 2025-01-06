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

    public Task<long> RespondWithText(Guid conversationId, string text)
        => Client.RespondWithText(conversationId, text);

    public Task<long> RespondWithKeyboard(Guid conversationId, Keyboard keyboard, string? text)
        => Client.RespondWithKeyboard(conversationId, keyboard, text);

    public Task AnswerKeyboardResponse(Guid conversationId, KeyboardResponse keyboardResponse, string? alertMessage, bool showAlert = false, int cacheTime = 0)
        => Client.AnswerKeyboardResponse(conversationId, keyboardResponse, alertMessage, showAlert, cacheTime);

    public Task EditKeyboard(Guid conversationId, long messageId, Keyboard newKeyboard, string? newText)
        => Client.EditKeyboard(conversationId, messageId, newKeyboard, newText);

    public Task EditText(Guid conversationId, long messageId, string newText)
        => Client.EditText(conversationId, messageId, newText);

    public Task DeleteMessage(Guid conversationId, long messageId)
        => Client.DeleteMessage(conversationId, messageId);

    public Task PrepareBot()
        => Client.PrepareBot();
}
