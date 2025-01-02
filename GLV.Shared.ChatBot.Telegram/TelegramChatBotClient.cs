using GLV.Shared.ChatBot;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Telegram.Bot.Types;

namespace GLV.Shared.ChatBot.Telegram;

public class TelegramChatBotClient(string botId, WTelegram.Bot client) : IChatBotClient
{
    public WTelegram.Bot BotClient { get; } = client ?? throw new ArgumentNullException(nameof(client));
    public string BotId { get; } = botId;
    public object UnderlyingBotClientObject => BotClient;

    public Task SetBotCommands(IEnumerable<ConversationActionDefinition> commands)
    {
        if (commands.Any() is false)
            return Task.CompletedTask;

        return BotClient.SetMyCommands(commands.Select(c => new BotCommand()
        {
            Command = PrepareCommand(c.CommandTrigger 
                ?? throw new ArgumentException("Encountered a command definition with a null command trigger", nameof(commands))),
            Description = c.CommandDescription ?? ""
        }));
    }

    private static string PrepareCommand(string trigger)
        => (trigger.StartsWith('/') ? '/' + trigger : trigger).Trim();

    public bool IsValidBotCommand(string text, [NotNullWhen(true)] out string? cmd)
    {
        if (TelegramRegexes.CheckCommandRegex.IsMatch(text))
        {
            cmd = text[1..];
            return true;
        }

        cmd = null;
        return false;
    }

    public async Task RespondWithText(Guid conversationId, string text)
    {
        var id = conversationId.UnpackTelegramConversationId();
        await BotClient.SendMessage(id, text);
    }

    public Task SetBotDescription(string name, string? shortDescription = null, string? description = null, CultureInfo? culture = null)
        => BotClient.SetMyInfo(name ?? BotId, shortDescription, description, (culture ?? CultureInfo.CurrentCulture).TwoLetterISOLanguageName);
}
