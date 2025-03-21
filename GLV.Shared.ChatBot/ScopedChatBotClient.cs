﻿using System.Diagnostics.CodeAnalysis;
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

    public Task SetBotCommands(IEnumerable<ConversationActionInformation> commands)
        => Client.SetBotCommands(commands);

    public Task SetBotDescription(string name, string? shortDescription = null, string? description = null, CultureInfo? culture = null)
        => Client.SetBotDescription(name, shortDescription, description, culture);

    public Task AnswerKeyboardResponse(Guid conversationId, KeyboardResponse keyboardResponse, string? alertMessage, bool showAlert = false, int cacheTime = 0)
        => Client.AnswerKeyboardResponse(conversationId, keyboardResponse, alertMessage, showAlert, cacheTime);

    public Task DeleteMessage(Guid conversationId, long messageId)
        => Client.DeleteMessage(conversationId, messageId);

    public Task PrepareBot()
        => Client.PrepareBot();

    public Task ProcessUpdate(UpdateContext update, ConversationContext context)
        => Client.ProcessUpdate(update, context);

    public Task<long> SendMessage(
        Guid conversationId, 
        string? text, 
        Keyboard? keyboard, 
        long? respondingToMessageId = null,
        IEnumerable<MessageAttachment>? attachments = null,
        MessageOptions options = default
    ) => Client.SendMessage(conversationId, text, keyboard, respondingToMessageId, attachments, options);

    public Task EditMessage(Guid conversationId, long messageId, string? newText, Keyboard? newKeyboard, MessageOptions options = default)
        => Client.EditMessage(conversationId, messageId, newText, newKeyboard, options);

    public bool IsReferringToBot(string text)
        => Client.IsReferringToBot(text);

    public bool ContainsReferenceToBot(string text)
        => Client.ContainsReferenceToBot(text);

    public bool TryGetTextAfterReferenceToBot(string text, out ReadOnlySpan<char> rest)
        => Client.TryGetTextAfterReferenceToBot(text, out rest);

    public SupportedFeatures SupportedFeatures => Client.SupportedFeatures;
}
