using GLV.Shared.ChatBot;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.RegularExpressions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using TL;
using WTelegram;
using WTelegram.Types;
using BotCommand = Telegram.Bot.Types.BotCommand;
using Update = WTelegram.Types.Update;

namespace GLV.Shared.ChatBot.Telegram;

public class TelegramChatBotClient : IChatBotClient
{
    public TelegramChatBotClient(
        string botId, 
        WTelegram.Bot client, 
        ChatBotManager manager,
        Func<Update, Task>? updateHandler = null,
        Func<Update, IChatBotClient, Guid>? conversationIdFactory = null
)
    {
        ConversationIdFactory = conversationIdFactory;
        UpdateHandler = updateHandler;
        Manager = manager ?? throw new ArgumentNullException(nameof(manager));
        BotClient = client ?? throw new ArgumentNullException(nameof(client));
        BotId = botId;
        BotClient.OnUpdate += BotClient_OnUpdate;
    }

    private Task BotClient_OnUpdate(WTelegram.Types.Update arg) 
        => UpdateHandler is not null
            ? UpdateHandler.Invoke(arg)
            : Manager.SubmitUpdate(new TelegramUpdateContext(arg, this, ConversationIdFactory));

    public ChatBotManager Manager { get; }
    public WTelegram.Bot BotClient { get; }
    public string BotId { get; }
    public object UnderlyingBotClientObject => BotClient;
    public Func<Update, IChatBotClient, Guid>? ConversationIdFactory { get; }
    public Func<Update, Task>? UpdateHandler { get; }

    private Regex CheckCommandRegex;

    public string BotHandle 
    {
        get => field ?? throw new InvalidOperationException("Cannot obtain the handle of this bot before it has been obtained via PrepareBot");
        private set; 
    }

    public async Task PrepareBot()
    {
        var me = await BotClient.GetMe();
        BotHandle = me.Username!;
        CheckCommandRegex = TelegramRegexes.CheckCommandRegex(BotHandle);
        Debug.Assert(CheckCommandRegex != null);
    }

    public Task SetBotCommands(IEnumerable<ConversationActionDefinition> commands)
    {
        if (commands.Any() is false)
            return Task.CompletedTask;

        try
        {
            return BotClient.SetMyCommands(commands.Select(c => new BotCommand()
            {
                Command = PrepareCommand(c.CommandTrigger 
                    ?? throw new ArgumentException("Encountered a command definition with a null command trigger", nameof(commands))),
                Description = c.CommandDescription ?? ""
            }).Append(new BotCommand()
            {
                Command = "/cancel",
                Description = "Cancels the current action and resets the conversation"
            }));
        }
        catch(RpcException excp)
        {
            if (excp.Message.Contains("FLOOD", StringComparison.OrdinalIgnoreCase))
                return Task.CompletedTask;
            throw;
        }
    }

    private static string PrepareCommand(string trigger)
        => (trigger.StartsWith('/') ? '/' + trigger : trigger).Trim();

    public bool IsValidBotCommand(string text, [NotNullWhen(true)] out string? cmd)
    {
        var match = CheckCommandRegex.Match(text);
        if (match is not null && match.Success && match.Groups.TryGetValue("cmd", out var grp))
        {
            cmd = grp.Value;
            return true;
        }

        cmd = null;
        return false;
    }

    public async Task<long> RespondWithText(Guid conversationId, string text)
    {
        var id = conversationId.UnpackTelegramConversationId();
        return (await BotClient.SendMessage(id, text)).Id;
    }

    public async Task SetBotDescription(string name, string? shortDescription = null, string? description = null, CultureInfo? culture = null)
    {
        try
        {
            await BotClient.SetMyInfo(name ?? BotId, shortDescription, description, (culture ?? CultureInfo.CurrentCulture).TwoLetterISOLanguageName);
        }
        catch (RpcException excp)
        {
            if (excp.Message.Contains("FLOOD", StringComparison.OrdinalIgnoreCase))
                return;
            throw;
        }
    }

    public async Task<long> RespondWithKeyboard(Guid conversationId, Keyboard keyboard, string? text)
    {
        var msg = await BotClient.SendMessage(
            conversationId.UnpackTelegramConversationId(),
            text ?? " ",
            replyMarkup: ParseKeyboard(keyboard)
        );
        return msg.Id;
    }

    public Task AnswerKeyboardResponse(Guid conversationId, KeyboardResponse keyboardResponse, string? alertMessage, bool showAlert, int cacheTime = 0)
        => BotClient.AnswerCallbackQuery(
            keyboardResponse.KeyboardId,
            alertMessage,
            showAlert,
            null,
            cacheTime
        );

    public Task EditKeyboard(Guid conversationId, long messageId, Keyboard newKeyboard, string? newText)
    {
        var chatId = conversationId.UnpackTelegramConversationId();
        var kb = ParseKeyboard(newKeyboard);
        var mid = (int)messageId;

        return string.IsNullOrWhiteSpace(newText) is false
            ? BotClient.EditMessageText(chatId, mid, newText)
            : (Task)BotClient.EditMessageReplyMarkup(chatId, mid, kb);
    }

    public Task EditText(Guid conversationId, long messageId, string newText)
        => BotClient.EditMessageText(conversationId.UnpackTelegramConversationId(), (int)messageId, newText);

    public static InlineKeyboardMarkup ParseKeyboard(Keyboard keyboard)
        => new(keyboard.Rows.Select(x => x.Keys.Select(y => new InlineKeyboardButton(y.Text) { CallbackData = y.Data })));

    [ThreadStatic]
    private static int[]? msgIdArray;
    public Task DeleteMessage(Guid conversationId, long messageId)
    {
        (msgIdArray ??= new int[1])[0] = (int)messageId;
        return BotClient.DeleteMessages(conversationId.UnpackTelegramConversationId(), msgIdArray);
    }
}
