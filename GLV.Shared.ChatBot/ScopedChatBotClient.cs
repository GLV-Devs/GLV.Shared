using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace GLV.Shared.ChatBot;

public class ScopedChatBotClient(Guid scopedConversation, IChatBotClient client) : IScopedChatBotClient
{
    public IChatBotClient ParentClient { get; } = client ?? throw new ArgumentNullException(nameof(client));

    public Guid ScopedConversation { get; } = scopedConversation;

    public string BotId => ParentClient.BotId;

    public object UnderlyingBotClientObject => ParentClient.UnderlyingBotClientObject;

    public bool IsValidBotCommand(string text, [NotNullWhen(true)] out string? commandName)
        => ParentClient.IsValidBotCommand(text, out commandName);

    public Task SetBotCommands(IEnumerable<ConversationActionInformation> commands)
        => ParentClient.SetBotCommands(commands);

    public Task SetBotDescription(string name, string? shortDescription = null, string? description = null, CultureInfo? culture = null)
        => ParentClient.SetBotDescription(name, shortDescription, description, culture);

    public Task AnswerKeyboardResponse(Guid conversationId, KeyboardResponse keyboardResponse, string? alertMessage, bool showAlert = false, int cacheTime = 0)
        => ParentClient.AnswerKeyboardResponse(conversationId, keyboardResponse, alertMessage, showAlert, cacheTime);

    public Task DeleteMessage(Guid conversationId, long messageId)
        => ParentClient.DeleteMessage(conversationId, messageId);

    public Task PrepareBot()
        => ParentClient.PrepareBot();

    public Task ProcessUpdate(UpdateContext update, ConversationContext context)
        => ParentClient.ProcessUpdate(update, context);

    public Task<long> SendMessage(
        Guid conversationId, 
        string? text, 
        Keyboard? keyboard, 
        long? respondingToMessageId = null,
        IEnumerable<MessageAttachment>? attachments = null,
        MessageOptions options = default
    ) => ParentClient.SendMessage(conversationId, text, keyboard, respondingToMessageId, attachments, options);

    public Task EditMessage(Guid conversationId, long messageId, string? newText, Keyboard? newKeyboard, MessageOptions options = default)
        => ParentClient.EditMessage(conversationId, messageId, newText, newKeyboard, options);

    public bool IsReferringToBot(string text)
        => ParentClient.IsReferringToBot(text);

    public bool ContainsReferenceToBot(string text)
        => ParentClient.ContainsReferenceToBot(text);

    public bool TryGetTextAfterReferenceToBot(string text, out ReadOnlySpan<char> rest)
        => ParentClient.TryGetTextAfterReferenceToBot(text, out rest);

    public SupportedFeatures SupportedFeatures => ParentClient.SupportedFeatures;

    public string Platform => ParentClient.Platform;

    public Task KickUser(Guid conversationId, long userId, string? reason = null)
        => ParentClient.KickUser(conversationId, userId, reason);

    public Task BanUser(Guid conversationId, long userId, int prune = 0, string? reason = null)
        => ParentClient.BanUser(conversationId, userId, prune, reason);

    public Task MuteUser(Guid conversationId, long userId, string? reason = null)
        => ParentClient.MuteUser(conversationId, userId, reason);

    public Task UnmuteUser(Guid conversationId, long userId, string? reason = null)
        => ParentClient.UnmuteUser(conversationId, userId, reason);

    public Task UnbanUser(Guid conversationId, long userId, string? reason = null)
        => ParentClient.UnbanUser(conversationId, userId, reason);

    public BotStatusSet StatusCollection => ParentClient.StatusCollection;

    public string BotMention => ParentClient.BotMention;

    public bool TryGetBotHandle([NotNullWhen(true)] out string? handle)
        => ParentClient.TryGetBotHandle(out handle);
}
