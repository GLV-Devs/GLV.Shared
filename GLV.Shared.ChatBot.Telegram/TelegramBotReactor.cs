using Telegram.Bot.Types;

namespace GLV.Shared.ChatBot.Telegram;

public class TelegramBotReactor
{
    protected readonly Func<Update, IChatBotClient, Guid>? ConversationIdFactory;

    public TelegramChatBotClient Client { get; }
    public ChatBotManager Manager { get; }

    public TelegramBotReactor(TelegramChatBotClient bot, ChatBotManager manager, Func<Update, IChatBotClient, Guid>? conversationIdFactory = null)
    {
        Client = bot ?? throw new ArgumentNullException(nameof(manager));
        Manager = manager ?? throw new ArgumentNullException(nameof(manager));
        ConversationIdFactory = conversationIdFactory;
        Client.BotClient.OnUpdate += Client_OnUpdate;
    }

    protected virtual async Task Client_OnUpdate(global::Telegram.Bot.Update arg)
    {
        await Manager.SubmitUpdate(new TelegramUpdateContext(arg, Client, ConversationIdFactory));
    }
}
