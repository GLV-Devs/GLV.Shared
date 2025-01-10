using GLV.Shared.ChatBot.Pipeline;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GLV.Shared.ChatBot;

public readonly record struct Message(string? Text, long MessageId, bool HasExtraData);
public readonly record struct KeyboardKey(string Text, string? Data);
public readonly record struct KeyboardRow(params IEnumerable<KeyboardKey> Keys);
public readonly record struct Keyboard(params IEnumerable<KeyboardRow> Rows);

public readonly record struct KeyboardResponse(string KeyboardId, string? Data)
{
    public bool MatchDataTag(string data)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(data);
        return Data == data;
    }
}

public abstract class UpdateContext(IChatBotClient client, Guid conversationId, string platform)
{
    public const string TelegramPlatform = "telegram-bot";
    public const string WhatsAppPlatform = "whatsapp-bot";
    public const string DiscordPlatform = "discord-bot";
    public const string TwitterPlatform = "twitter-bot";

    internal PipelineContext? pipelineContext;

    public virtual bool IsHandledByBotClient { get; }

    public string Platform { get; } = platform;
    public Guid ConversationId { get; } = conversationId;
    public IChatBotClient Client { get; } = client;

    public abstract KeyboardResponse? KeyboardResponse { get; }
    public abstract Message? Message { get; }
}
