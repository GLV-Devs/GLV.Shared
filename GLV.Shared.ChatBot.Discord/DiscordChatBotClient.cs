using Discord;
using Discord.Commands;
using Discord.WebSocket;
using GLV.Shared.ChatBot.Discord.DiscordUpdates;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.RegularExpressions;
using static System.Net.Mime.MediaTypeNames;

namespace GLV.Shared.ChatBot.Discord;

public abstract class DiscordChatBotClient : IChatBotClient
{
    internal DiscordChatBotClient(
        string botId,
        IDiscordClient client,
        ChatBotManager manager,
        CommandService commandService,
        Func<DiscordUpdateContext, ChatBotManager, Task>? updateHandler = null
    )
    {
        Manager = manager ?? throw new ArgumentNullException(nameof(manager));
        BotClient = client ?? throw new ArgumentNullException(nameof(client));
        CommandService = commandService ?? throw new ArgumentNullException(nameof(commandService));
        BotId = botId;
        UpdateHandler = updateHandler;
    }

    protected Task SubmitUpdate(DiscordUpdateContext context)
        => UpdateHandler is not null ? UpdateHandler.Invoke(context, Manager) : Manager.SubmitUpdate(context);

    protected virtual async Task BotClient_CommandUpdate(ICommandContext context, object[] args, IServiceProvider? services, CommandInfo commandInfo)
    {
        var update = new DiscordCommandUpdateContext(this, context, commandInfo, context.PackDiscordConversationId())
        {
            JumpToActiveAction = commandInfo.Aliases[0],
            JumpToActiveActionStep = 0
        };

        if (UpdateHandler is not null)
            await UpdateHandler.Invoke(update, Manager);
        else
            await Manager.SubmitUpdate(update);
    }

    public ChatBotManager Manager { get; }
    public IDiscordClient BotClient { get; }
    public CommandService CommandService { get; }
    public string BotId { get; }
    public object UnderlyingBotClientObject => BotClient;
    public Func<DiscordUpdateContext, ChatBotManager, Task>? UpdateHandler { get; }

    public bool AllowReactionsInPlaceOfInlineKeyboard { get; init; } = false;

    [field: AllowNull]
    public string BotHandle
    {
        get => field ?? throw new InvalidOperationException("Cannot obtain the handle of this bot before it has been obtained via PrepareBot");
        private set;
    }

    private ulong? ___botId;
    public ulong DiscordBotId
    {
        get => ___botId ?? throw new InvalidOperationException("Cannot obtain the handle of this bot before it has been obtained via PrepareBot");
        private set => ___botId = value;
    }

    public virtual Task PrepareBot()
    {
        var me = BotClient.CurrentUser;
        BotHandle = me.Username!;
        DiscordBotId = me.Id!;
        return Task.CompletedTask;
    }

    public async Task SetBotCommands(IEnumerable<ConversationActionInformation> commands)
    {
        if (commands.Any() is false)
            return;

        await CommandService.CreateModuleAsync("ChatBotCommands", mb =>
        {
            foreach (var cmd in commands)
            {
                mb.AddCommand(cmd.CommandTrigger, BotClient_CommandUpdate, cb =>
                {
                    cb.Name = cmd.ActionName;
                    cb.RunMode = RunMode.Async;
                    cb.Summary = cmd.CommandDescription;
                });
            }
        });
    }

    public bool IsValidBotCommand(string text, [NotNullWhen(true)] out string? cmd)
    {
        cmd = text;
        return true;
        /*
        var match = CheckCommandRegex.Match(text);
        if (match is not null && match.Success && match.Groups.TryGetValue("cmd", out var grp))
        {
            cmd = grp.Value;
            return true;
        }

        cmd = null;
        return false;
        */
    }

    public Task SetBotDescription(string name, string? shortDescription = null, string? description = null, CultureInfo? culture = null)
        => Task.CompletedTask;

    public async Task<long> SendMessage(Guid conversationId, string? text, Keyboard? kr, IEnumerable<MessageAttachment>? attachments, MessageOptions options = default)
    {
        conversationId.UnpackDiscordConversationId(out var channel);

        if (kr is Keyboard keyboard)
        {
            if (AllowReactionsInPlaceOfInlineKeyboard)
                throw new NotImplementedException();
            else
                throw new NotSupportedException("Keyboards are not supported in DiscordChatBotClient. Try setting AllowReactionsInPlaceOfInlineKeyboard to true");
        }
        else
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(text);

            var ch = await BotClient.GetChannelAsync(channel)
                ?? throw new InvalidOperationException($"Could not find a channel under id {channel} that this bot has access to");

            if (ch is not IMessageChannel messageChannel)
                throw new InvalidOperationException($"The channel under id {channel} is not a message channel");
            else
            {
                return attachments is not null && attachments.Any()
                    ? (long)(await messageChannel.SendFilesAsync(attachments.Select(a => new FileAttachment(
                        a.GetContent(),
                        a.AttachmentTitle ?? "notitle",
                        a.Description,
                        a.IsSpoiler,
                        a.IsThumbnail,
                        a.Duration
                    )), text, flags: options.SendWithoutNotification ? MessageFlags.SuppressNotification : 0)).Id
                    : (long)(await messageChannel.SendMessageAsync(text)).Id;
            }
        }
    }

    public Task AnswerKeyboardResponse(Guid conversationId, KeyboardResponse keyboardResponse, string? alertMessage, bool showAlert, int cacheTime = 0)
    {
        if (AllowReactionsInPlaceOfInlineKeyboard)
            throw new NotImplementedException();
        else
            throw new NotSupportedException("Keyboards are not supported in DiscordChatBotClient. Try setting AllowReactionsInPlaceOfInlineKeyboard to true");
    }

    public async Task EditMessage(Guid conversationId, long messageId, string? newText, Keyboard? newKeyboard, MessageOptions options = default)
    {
        conversationId.UnpackDiscordConversationId(out var channel);

        if (newKeyboard is Keyboard keyboard)
        {
            if (AllowReactionsInPlaceOfInlineKeyboard)
                throw new NotImplementedException();
            else
                throw new NotSupportedException("Keyboards are not supported in DiscordChatBotClient. Try setting AllowReactionsInPlaceOfInlineKeyboard to true");
        }
        else
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(newText);

            var ch = await BotClient.GetChannelAsync(channel)
                ?? throw new InvalidOperationException($"Could not find a channel under id {channel} that this bot has access to");

            if (ch is not IMessageChannel messageChannel)
                throw new InvalidOperationException($"The channel under id {channel} is not a message channel");

            await messageChannel.ModifyMessageAsync((ulong)messageId, f => f.Content = newText);
        }
    }

    public async Task DeleteMessage(Guid conversationId, long messageId)
    {
        conversationId.UnpackDiscordConversationId(out var channel);

        var ch = await BotClient.GetChannelAsync(channel)
            ?? throw new InvalidOperationException($"Could not find a channel under id {channel} that this bot has access to");

        if (ch is not IMessageChannel messageChannel)
            throw new InvalidOperationException($"The channel under id {channel} is not a message channel");

        await messageChannel.DeleteMessageAsync((ulong)messageId);
    }

    public virtual Task ProcessUpdate(UpdateContext uc, ConversationContext context)
    {
        return Task.CompletedTask;
    }

    public bool IsReferringToBot(string text)
        => text.Equals($"@{BotHandle}", StringComparison.OrdinalIgnoreCase)
        || text.Equals($"<@{DiscordBotId}>", StringComparison.OrdinalIgnoreCase);

    public bool ContainsReferenceToBot(string text)
        => text.Contains($"@{BotHandle}", StringComparison.OrdinalIgnoreCase)
        || text.Contains($"<@{DiscordBotId}>", StringComparison.OrdinalIgnoreCase);

    public bool TryGetTextAfterReferenceToBot(string text, out ReadOnlySpan<char> rest)
    {
        var testStr = $"@{BotHandle}";
        int indx = text.LastIndexOf(testStr, StringComparison.OrdinalIgnoreCase);

        if (indx is -1)
        {
            testStr = $"<@{DiscordBotId}>";
            indx = text.LastIndexOf(testStr, StringComparison.OrdinalIgnoreCase);
        }

        if (indx is -1)
        {
            rest = default;
            return false;
        }

        rest = text.AsSpan()[(indx + (testStr.Length - 1))..];
        return true;
    }

    public SupportedFeatures SupportedFeatures { get; } = new()
    {
        AudioMessageAttachment = true,
        ImageMessageAttachment = true,
        InlineKeyboards = false,
        MessageAttachments = true,
        VideoMessageAttachment = true,
        SpoilerAttachments = true,
        AttachmentTitles = true,
        ThumbnailAttachments = true,
        AttachmentDuration = true,
        MultipleAttachments = true,
        AttachmentDescriptions = true,
        HtmlText = false,
        ProtectMediaContent = false,
        SendWithoutNotification = true
    };
}
