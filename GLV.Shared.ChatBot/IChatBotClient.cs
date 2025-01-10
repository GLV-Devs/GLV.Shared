using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace GLV.Shared.ChatBot;

public interface IScopedChatBotClient : IChatBotClient
{
    public Guid ScopedConversation { get; }

    public Task<long> SendMessage(string? text, Keyboard? keyboard = null)
        => SendMessage(ScopedConversation, text, keyboard);

    public Task AnswerKeyboardResponse(KeyboardResponse keyboardResponse, string? alertMessage, bool showAlert = false, int cacheTime = 0)
        => AnswerKeyboardResponse(ScopedConversation, keyboardResponse, alertMessage, showAlert, cacheTime);

    public Task DeleteMessage(long messageId)
        => DeleteMessage(ScopedConversation, messageId);

    public Task EditMessage(long messageId, string? newText, Keyboard? newKeyboard = null)
        => EditMessage(ScopedConversation, messageId, newText, newKeyboard);

    public Task<bool> TryDeleteMessage(long messageId)
        => TryDeleteMessage(ScopedConversation, messageId);
}

public interface IChatBotClient
{
    public string BotId { get; }
    public object UnderlyingBotClientObject { get; }

    public bool IsValidBotCommand(string text, [NotNullWhen(true)] out string? commandName);
    public Task SetBotCommands(IEnumerable<ConversationActionInformation> commands);
    public Task SetBotDescription(string name, string? shortDescription = null, string? description = null, CultureInfo? culture = null);
    public Task AnswerKeyboardResponse(Guid conversationId, KeyboardResponse keyboardResponse, string? alertMessage, bool showAlert = false, int cacheTime = 0);
    public Task<long> SendMessage(Guid conversationId, string? text, Keyboard? keyboard = null);
    public Task EditMessage(Guid conversationId, long messageId, string? newText, Keyboard? newKeyboard = null);
    public Task DeleteMessage(Guid conversationId, long messageId);

    public async Task<bool> TryDeleteMessage(Guid conversationId, long messageId)
    {
        try
        {
            await DeleteMessage(conversationId, messageId);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public Task ProcessUpdate(UpdateContext update, ConversationContext context);
    public Task PrepareBot();
    public bool IsReferringToBot(string text);
    public bool ContainsReferenceToBot(string text);
}
