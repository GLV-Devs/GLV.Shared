using GLV.Shared.ChatBot;
using WTelegram.Types;

namespace GLV.Shared.ChatBot.Telegram;

public class TelegramUpdateContext(
    Update update,
    IChatBotClient client,
    Func<Update, IChatBotClient, Guid>? conversationIdFactory = null
) : UpdateContext(client, conversationIdFactory?.Invoke(update, client) ?? GetConversationId(update, client), TelegramPlatform)
{
    public Update Update { get; } = update;

    public static Guid GetConversationId(Update update, IChatBotClient client)
        => update.GetTelegramConversationId(client.BotId);
}
