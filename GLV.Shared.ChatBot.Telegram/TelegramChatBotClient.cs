using GLV.Shared.ChatBot;

namespace GLV.Shared.ChatBot.Telegram;

public class TelegramChatBotClient(string botId, WTelegram.Bot client) : IChatBotClient
{
    public WTelegram.Bot BotClient { get; } = client ?? throw new ArgumentNullException(nameof(client));
    public string BotId { get; } = botId;
    public object UnderlyingBotClientObject => BotClient;
}
