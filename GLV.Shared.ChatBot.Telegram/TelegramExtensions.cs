using GLV.Shared.Common;
using System.Runtime.InteropServices;
using WTelegram.Types;
using Telegram.Bot.Types.Enums;
using System.Runtime.CompilerServices;
using Telegram.Bot.Types;
using TLMessage = Telegram.Bot.Types.Message;

namespace GLV.Shared.ChatBot.Telegram;

public static class TelegramExtensions
{
    public static MessageReference? GetMessageReference(this TLMessage? receivedMessage)
        => receivedMessage is null || receivedMessage.ReplyToMessage is not TLMessage refMsg
        ? null
        : new MessageReference(
            refMsg.Id,
            refMsg.Text,
            refMsg.From?.GetUserInfo(),
            refMsg.Type != MessageType.Text,
            refMsg
        );

    public static UserInfo GetUserInfo(this WTelegram.Types.User u)
        => new(
            u.Username,
            string.IsNullOrWhiteSpace(u.FirstName) is false
                ? string.IsNullOrWhiteSpace(u.LastName) is false
                    ? $"{u.FirstName} {u.LastName}"
                    : u.FirstName
                : null,
            u.PackTelegramUserId(),
            u
        );

    public static UserInfo GetUserInfo(this global::Telegram.Bot.Types.User u)
        => new(
            u.Username,
            string.IsNullOrWhiteSpace(u.FirstName) is false
                ? string.IsNullOrWhiteSpace(u.LastName) is false
                    ? $"{u.FirstName} {u.LastName}"
                    : u.FirstName
                : null,
            u.PackTelegramUserId(),
            u
        );

    public static Guid PackTelegramUserId(this WTelegram.Types.User user)
        => MemoryMarshal.Cast<long, Guid>([0, user.Id])[0];

    public static Guid PackTelegramUserId(this global::Telegram.Bot.Types.User user)
        => MemoryMarshal.Cast<long, Guid>([0, user.Id])[0];

    public static long UnpackTelegramConversationId(this Guid conversationId)
        => MemoryMarshal.Cast<Guid, long>(MemoryMarshal.CreateSpan(ref conversationId, 1))[0];

    public static long UnpackTelegramUserId(this Guid userId)
        => MemoryMarshal.Cast<Guid, long>(MemoryMarshal.CreateSpan(ref userId, 1))[1];

    public static string UnpackTelegramPollTruncatedMD5HashId(this Guid conversationId)
        => MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref conversationId, 1))[..sizeof(long)].ToHexViaLookup32();

    /// <summary>
    /// Gets a conversation id based off of a Telegram Chat Id
    /// </summary>
    /// <param name="chatId">The telegram chat id</param>
    /// <param name="botId">An arbitrary id, or unique name for the bot. If <see langword="null"/>, <paramref name="chatId"/> will be used at both heights of the <see cref="Guid"/></param>
    /// <remarks>
    /// If you want to support conversations across platforms, consider using a different indexing scheme; such as simply using <see cref="Guid.NewGuid()"/> and having a separate set that relates platform specific chat Ids to this Guid
    /// </remarks>
    public static Guid GetTelegramMessageConversationId(long chatId, string? botId)
    {
        Guid id = default;
        if (string.IsNullOrWhiteSpace(botId))
        {
            var span = MemoryMarshal.Cast<Guid, long>(MemoryMarshal.CreateSpan(ref id, 1));
            span[0] = chatId;
            span[1] = chatId;
        }
        else
        {
            var span = MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref id, 1));
            Span<byte> md5buffer = stackalloc byte[Unsafe.SizeOf<Guid>()];
            botId.TryHashToMD5(md5buffer);
            md5buffer[..sizeof(long)].CopyTo(span[sizeof(long)..]);
            MemoryMarshal.Cast<Guid, long>(MemoryMarshal.CreateSpan(ref id, 1))[0] = chatId;
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
        {
            Span<byte> md5buffer = stackalloc byte[Unsafe.SizeOf<Guid>()];
            botId.TryHashToMD5(md5buffer);
            md5buffer[..sizeof(long)].CopyTo(span[sizeof(long)..]);
        }

        return id;
    }

    public static Guid GetTelegramConversationId(this global::Telegram.Bot.Types.Update update, string? chatBotId = null) => update.Type switch
    {
        UpdateType.Message => GetTelegramMessageConversationId(update.Message!.Chat.Id, chatBotId),
        UpdateType.InlineQuery => GetTelegramMessageConversationId(update.InlineQuery!.From.Id, chatBotId),
        UpdateType.ChosenInlineResult => GetTelegramMessageConversationId(update.ChosenInlineResult!.From.Id, chatBotId),
        UpdateType.CallbackQuery => GetTelegramMessageConversationId(update.CallbackQuery!.Message!.Chat.Id, chatBotId),
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
