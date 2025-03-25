using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace GLV.Shared.ChatBot;

public interface IScopedChatBotClient : IChatBotClient
{
    public Guid ScopedConversation { get; }

    public Task<long> SendMessage(string? text, Keyboard? keyboard = null,
        long? respondingToMessageId = null, IEnumerable<MessageAttachment>? attachments = null, MessageOptions options = default)
        => SendMessage(ScopedConversation, text, keyboard, respondingToMessageId, attachments, options);

    public Task AnswerKeyboardResponse(KeyboardResponse keyboardResponse, string? alertMessage, bool showAlert = false, int cacheTime = 0)
        => AnswerKeyboardResponse(ScopedConversation, keyboardResponse, alertMessage, showAlert, cacheTime);

    public Task DeleteMessage(long messageId)
        => DeleteMessage(ScopedConversation, messageId);

    public Task EditMessage(long messageId, string? newText, Keyboard? newKeyboard = null, MessageOptions options = default)
        => EditMessage(ScopedConversation, messageId, newText, newKeyboard, options);

    public Task KickUser(long userId, string? reason = null)
        => KickUser(ScopedConversation, userId, reason);

    /// <summary>
    /// Attempts to ban the user from the conversation
    /// </summary>
    /// <param name="userId">The userId of the user to ban</param>
    /// <param name="prune">The value of pruning. This value can mean different things on different platforms. On Discord, it's the amount of historical days to scan for messages to delete. On Telegram, any value above zero will delete all messages</param>
    /// <param name="reason">The reason for the ban</param>
    public Task BanUser(long userId, int prune = 0, string? reason = null)
        => BanUser(ScopedConversation, userId, prune, reason);

    public Task MuteUser(long userId, string? reason = null)
        => MuteUser(ScopedConversation, userId, reason);

    public Task UnmuteUser(long userId, string? reason = null)
        => UnmuteUser(ScopedConversation, userId, reason);

    public Task UnbanUser(long userId, string? reason = null)
        => UnbanUser(ScopedConversation, userId, reason);
}

public interface IChatBotClient
{
    public string BotId { get; }
    public object UnderlyingBotClientObject { get; }

    public string Platform { get; }

    public SupportedFeatures SupportedFeatures { get; }

    public bool IsValidBotCommand(string text, [NotNullWhen(true)] out string? commandName);
    public Task SetBotCommands(IEnumerable<ConversationActionInformation> commands);
    public Task SetBotDescription(string name, string? shortDescription = null, string? description = null, CultureInfo? culture = null);
    public Task AnswerKeyboardResponse(Guid conversationId, KeyboardResponse keyboardResponse, string? alertMessage, bool showAlert = false, int cacheTime = 0);
    public Task<long> SendMessage(Guid conversationId, string? text, Keyboard? keyboard = null,
        long? respondingToMessageId = null, IEnumerable<MessageAttachment>? attachments = null, MessageOptions options = default);
    public Task EditMessage(Guid conversationId, long messageId, string? newText, Keyboard? newKeyboard = null, MessageOptions options = default);
    public Task DeleteMessage(Guid conversationId, long messageId);

    public Task KickUser(Guid conversationId, long userId, string? reason = null);

    /// <summary>
    /// Attempts to ban the user from the conversation
    /// </summary>
    /// <param name="userId">The userId of the user to ban</param>
    /// <param name="prune">The value of pruning. This value can mean different things on different platforms. On Discord, it's the amount of historical days to scan for messages to delete. On Telegram, any value above zero will delete all messages</param>
    /// <param name="reason">The reason for the ban</param>
    public Task BanUser(Guid conversationId, long userId, int prune = 0, string? reason = null);
    public Task MuteUser(Guid conversationId, long userId, string? reason = null);
    public Task UnmuteUser(Guid conversationId, long userId, string? reason = null);
    public Task UnbanUser(Guid conversationId, long userId, string? reason = null);
    public Task ProcessUpdate(UpdateContext update, ConversationContext context);
    public Task PrepareBot();
    public bool IsReferringToBot(string text);
    public bool ContainsReferenceToBot(string text);
    public bool TryGetTextAfterReferenceToBot(string text, out ReadOnlySpan<char> rest);
}
