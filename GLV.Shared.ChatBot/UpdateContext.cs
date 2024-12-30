using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GLV.Shared.ChatBot;
public abstract class UpdateContext(IChatBotClient client, Guid conversationId, string platform)
{
    public const string TelegramPlatform = "telegram-bot";
    public const string WhatsAppPlatform = "whatsapp-bot";
    public const string DiscordPlatform = "discord-bot";
    public const string TwitterPlatform = "twitter-bot";

    public string Platform { get; } = platform;
    public Guid ConversationId { get; } = conversationId;
    public IChatBotClient Client { get; } = client;
}
