using GLV.Shared.Common;
using System.Runtime.InteropServices;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace GLV.Shared.ChatBot.Telegram;

public static class TelegramExtensions
{
    /// <summary>
    /// Gets a conversation id based off of a Telegram Chat Id
    /// </summary>
    /// <param name="stringId">The telegram chat id</param>
    /// <param name="botId">An arbitrary id, or unique name for the bot. If <see langword="null"/>, <paramref name="stringId"/> will be used at both heights of the <see cref="Guid"/></param>
    /// <remarks>
    /// If you want to support conversations across platforms, consider using a different indexing scheme; such as simply using <see cref="Guid.NewGuid()"/> and having a separate set that relates platform specific chat Ids to this Guid
    /// </remarks>
    public static Guid GetTelegramMessageConversationId(long stringId, string? botId)
    {
        Guid id = default;
        if (string.IsNullOrWhiteSpace(botId))
        {
            var span = MemoryMarshal.Cast<Guid, long>(MemoryMarshal.CreateSpan(ref id, 1));
            span[0] = stringId;
            span[1] = stringId;
        }
        else
        {
            MemoryMarshal.Cast<Guid, long>(MemoryMarshal.CreateSpan(ref id, 1))[0] = stringId;
            var span = MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref id, 1));
            botId.TryHashToMD5(span[sizeof(long)..]);
        }
        return id;
    }

    /// <summary>
    /// Gets a conversation id based off of a Telegram Chat Id
    /// </summary>
    /// <param name="stringId">The telegram string id (These come from Polls, mostly. All other updates should have a long id instead)</param>
    /// <param name="botId">An arbitrary id, or unique name for the bot. If <see langword="null"/>, <paramref name="stringId"/> will be used at both heights of the <see cref="Guid"/></param>
    /// <remarks>
    /// If you want to support conversations across platforms, consider using a different indexing scheme; such as simply using <see cref="Guid.NewGuid()"/> and having a separate set that relates platform specific chat Ids to this Guid
    /// </remarks>
    public static Guid GetTelegramMessageConversationId(string stringId, string? botId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(stringId);
        Guid id = default;
        
        var span = MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref id, 1));
        stringId.TryHashToMD5(span);

        if (string.IsNullOrWhiteSpace(botId) is false)
            botId.TryHashToMD5(span[sizeof(long)..]);

        return id;
    }

    public static Guid GetTelegramConversationId(this Update update, string? chatBotId = null) => update.Type switch
    {
        UpdateType.Message => GetTelegramMessageConversationId(update.Message!.Chat.Id, chatBotId),
        UpdateType.InlineQuery => GetTelegramMessageConversationId(update.InlineQuery!.From.Id, chatBotId),
        UpdateType.ChosenInlineResult => GetTelegramMessageConversationId(update.ChosenInlineResult!.From.Id, chatBotId),
        UpdateType.CallbackQuery => GetTelegramMessageConversationId(update.CallbackQuery!.From.Id, chatBotId),
        UpdateType.EditedMessage => GetTelegramMessageConversationId(update.EditedMessage!.Chat.Id, chatBotId),
        UpdateType.ChannelPost => GetTelegramMessageConversationId(update.ChannelPost!.Chat.Id, chatBotId),
        UpdateType.EditedChannelPost => GetTelegramMessageConversationId(update.EditedChannelPost!.Chat.Id, chatBotId),
        UpdateType.ShippingQuery => GetTelegramMessageConversationId(update.ShippingQuery!.From.Id, chatBotId),
        UpdateType.PreCheckoutQuery => GetTelegramMessageConversationId(update.PreCheckoutQuery!.From.Id, chatBotId),
        UpdateType.Poll => GetTelegramMessageConversationId(update.Poll!.Id, chatBotId),
        UpdateType.PollAnswer => GetTelegramMessageConversationId(update.PollAnswer!.PollId, chatBotId),
        UpdateType.MyChatMember => GetTelegramMessageConversationId(update.MyChatMember!.Chat.Id, chatBotId),
        UpdateType.ChatMember => GetTelegramMessageConversationId(update.ChatMember!.Chat.Id, chatBotId),
        UpdateType.ChatJoinRequest => GetTelegramMessageConversationId(update.ChatJoinRequest!.Chat.Id, chatBotId),
        UpdateType.MessageReaction => GetTelegramMessageConversationId(update.MessageReaction!.Chat.Id, chatBotId),
        UpdateType.MessageReactionCount => GetTelegramMessageConversationId(update.MessageReactionCount!.Chat.Id, chatBotId),
        UpdateType.ChatBoost => GetTelegramMessageConversationId(update.ChatBoost!.Chat.Id, chatBotId),
        UpdateType.RemovedChatBoost => GetTelegramMessageConversationId(update.RemovedChatBoost!.Chat.Id, chatBotId),
        UpdateType.BusinessConnection => GetTelegramMessageConversationId(update.BusinessConnection!.UserChatId, chatBotId),
        UpdateType.BusinessMessage => GetTelegramMessageConversationId(update.BusinessMessage!.Chat.Id, chatBotId),
        UpdateType.EditedBusinessMessage => GetTelegramMessageConversationId(update.EditedBusinessMessage!.Chat.Id, chatBotId),
        UpdateType.DeletedBusinessMessages => GetTelegramMessageConversationId(update.DeletedBusinessMessages!.Chat.Id, chatBotId),
        UpdateType.PurchasedPaidMedia => GetTelegramMessageConversationId(update.PurchasedPaidMedia!.From.Id, chatBotId),
        _ => throw new ArgumentException($"Unsupported update type: {update.Type}", nameof(update))
    };
}
