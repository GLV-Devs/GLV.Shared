using GLV.Shared.ChatBot;
using System.Text.Json.Serialization;
using Telegram.Bot.Types.Enums;
using WTelegram.Types;

namespace GLV.Shared.ChatBot.Telegram;

public class TelegramUpdateContext(
    Update update,
    IChatBotClient client,
    Guid? conversationId = null,
    bool isHandledByBotClient = false
) : UpdateContext(client, conversationId ?? GetConversationId(update, client), TelegramPlatform)
{
    public Update Update { get; } = update;

    public override bool IsHandledByBotClient => isHandledByBotClient;

    public static Guid GetConversationId(Update update, IChatBotClient client)
        => update.GetTelegramConversationId(client.BotId);

    public override Message? Message { get; }
        = update.Message is null
        ? null
        : new Message(
            update.Message.Text, 
            update.Message.Id,
            update.Message.ReplyToMessage?.Id,
            update.Message.From is User u 
            ? new UserInfo(
                u.Username,
                string.IsNullOrWhiteSpace(u.FirstName) is false
                    ? string.IsNullOrWhiteSpace(u.LastName) is false
                        ? $"{u.FirstName} {u.LastName}"
                        : u.FirstName
                    : null,
                u.PackTelegramUserId()
            )
            : null,
            update.Message.Type != MessageType.Text
        );

    public override KeyboardResponse? KeyboardResponse { get; }
        = update.CallbackQuery is null
        ? null
        : new KeyboardResponse(update.CallbackQuery.Id, update.CallbackQuery.Data);
}
