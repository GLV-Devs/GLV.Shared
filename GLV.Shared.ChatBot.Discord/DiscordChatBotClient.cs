using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.RegularExpressions;
using static System.Net.Mime.MediaTypeNames;

namespace GLV.Shared.ChatBot.Discord;

public class DiscordChatBotClient(
    string botId,
    IDiscordClient client,
    ChatBotManager manager,
    CommandService commandService,
    Func<DiscordUpdateContext, ChatBotManager, Task>? updateHandler = null
) : IChatBotClient
{
    protected virtual async Task BotClient_CommandUpdate(ICommandContext context, object[] args, IServiceProvider? services, CommandInfo commandInfo)
    {
        var update = new DiscordCommandUpdateContext(this, context, context.PackDiscordGuildConversationId());
        update.AddOrReplaceFeature(commandInfo);

        if (UpdateHandler is not null)
            await UpdateHandler.Invoke(update, Manager);
        else
            await Manager.SubmitUpdate(update);
    }

    public ChatBotManager Manager { get; } = manager ?? throw new ArgumentNullException(nameof(manager));
    public IDiscordClient BotClient { get; } = client ?? throw new ArgumentNullException(nameof(client));
    public CommandService CommandService { get; } = commandService ?? throw new ArgumentNullException(nameof(commandService));
    public string BotId { get; } = botId;
    public object UnderlyingBotClientObject => BotClient;
    public Func<DiscordUpdateContext, ChatBotManager, Task>? UpdateHandler { get; } = updateHandler;

    public bool AllowReactionsInPlaceOfInlineKeyboard { get; init; } = false;

    [field: AllowNull]
    public string BotHandle 
    {
        get => field ?? throw new InvalidOperationException("Cannot obtain the handle of this bot before it has been obtained via PrepareBot");
        private set; 
    }

    public Task PrepareBot()
    {
        var me = BotClient.CurrentUser;
        BotHandle = me.Username!;
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
        conversationId.UnpackDiscordGuildConversationId(out var guild, out var channel);
        
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
        conversationId.UnpackDiscordGuildConversationId(out var guild, out var channel);

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
        conversationId.UnpackDiscordGuildConversationId(out var guild, out var channel);

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
        => text.Equals($"@{BotHandle}", StringComparison.OrdinalIgnoreCase);

    public bool ContainsReferenceToBot(string text)
        => text.Contains($"@{BotHandle}", StringComparison.OrdinalIgnoreCase);

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
