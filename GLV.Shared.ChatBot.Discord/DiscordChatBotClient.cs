using Discord;
using Discord.Commands;
using Discord.WebSocket;
using GLV.Shared.ChatBot.Discord.DiscordUpdates;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection.Emit;
using System.Text.RegularExpressions;
using System.Threading.Channels;
using static System.Net.Mime.MediaTypeNames;

namespace GLV.Shared.ChatBot.Discord;

public sealed record class DiscordEmbedBuilderSet(EmbedBuilder EmbedBuilder, ComponentBuilder ComponentBuilder);

public abstract class DiscordChatBotClient : IChatBotClient
{
    internal DiscordChatBotClient(
        string botId,
        IDiscordClient client,
        ChatBotManager manager,
        Func<DiscordUpdateContext, ChatBotManager, Task>? updateHandler = null
    )
    {
        Manager = manager ?? throw new ArgumentNullException(nameof(manager));
        BotClient = client ?? throw new ArgumentNullException(nameof(client));
        BotId = botId;
        UpdateHandler = updateHandler;
        StatusCollection = DiscordBotStatuses.CreateStatusCollection(this);
    }

    protected Task SubmitUpdate(DiscordUpdateContext context)
        => UpdateHandler is not null ? UpdateHandler.Invoke(context, Manager) : Manager.SubmitUpdate(context);

    public string Platform { get; } = UpdateContext.DiscordPlatform;
    public ChatBotManager Manager { get; }
    public IDiscordClient BotClient { get; }
    public string BotId { get; }
    public object UnderlyingBotClientObject => BotClient;
    public Func<DiscordUpdateContext, ChatBotManager, Task>? UpdateHandler { get; }

    [field: AllowNull]
    public string BotHandle
    {
        get => field ?? throw new InvalidOperationException("Cannot obtain the handle of this bot before it has been obtained via PrepareBot");
        private set;
    }

    [field: AllowNull]
    public string BotMention
    {
        get => field ?? throw new InvalidOperationException("Cannot obtain the Referrable Username of this bot before it has been obtained via PrepareBot");
        private set;
    }

    private ulong? ___botId;
    public ulong DiscordBotId
    {
        get => ___botId ?? throw new InvalidOperationException("Cannot obtain the handle of this bot before it has been obtained via PrepareBot");
        private set => ___botId = value;
    }

    public bool TryGetBotHandle([NotNullWhen(true)] out string? handle)
    {
        handle = BotHandle;
        return true;
    }

    private Regex? CheckCommandRegex;
    public virtual Task PrepareBot()
    {
        var me = BotClient.CurrentUser;
        BotHandle = me.Username!;
        DiscordBotId = me.Id!;
        BotMention = BotClient.CurrentUser.Mention;
        CheckCommandRegex = DiscordRegexes.CheckCommandRegex(BotMention);
        return Task.CompletedTask;
    }

    private readonly static InteractionContextType[] InteractionContextTypeArray = [
        InteractionContextType.Guild,
        InteractionContextType.PrivateChannel,
        InteractionContextType.BotDm
    ];

    public async Task SetBotCommands(IEnumerable<ConversationActionInformation> commands)
    {
        if (commands.Any() is false)
            return;

        List<ApplicationCommandProperties> properties = [];
        foreach (var cmd in commands)
        {
            properties.Add(
                new SlashCommandBuilder()
                .WithDescription(cmd.CommandDescription)
                .WithName(cmd.CommandTrigger)
                .WithContextTypes(InteractionContextTypeArray)
                .Build()
            );
        }

        properties.Add(
                new SlashCommandBuilder()
                .WithDescription("Cancels the current action and resets the conversation")
                .WithName("cancel")
                .WithContextTypes(InteractionContextTypeArray)
                .Build()
        );

        await BotClient.BulkOverwriteGlobalApplicationCommand([.. properties]);
    }

    public bool IsValidBotCommand(string text, [NotNullWhen(true)] out string? cmd)
    {
        Debug.Assert(CheckCommandRegex is not null);
        var match = CheckCommandRegex.Match(text);
        if (match is not null && match.Success && match.Groups.TryGetValue("cmd", out var grp))
        {
            cmd = grp.Value;
            return true;
        }

        cmd = null;
        return false;
    }

    public Task SetBotDescription(string name, string? shortDescription = null, string? description = null, CultureInfo? culture = null)
        => Task.CompletedTask;

    public async Task<long> SendMessage(Guid conversationId, string? text, Keyboard? kr, long? replyToMessageId, IEnumerable<MessageAttachment>? attachments, MessageOptions options = default)
    {
        conversationId.UnpackDiscordConversationId(out var guild, out var channel);

        var reply
            = replyToMessageId is long rtmi
            ? new global::Discord.MessageReference((ulong)rtmi)
            : null;

        var flags = options.SendWithoutNotification ? MessageFlags.SuppressNotification : 0;

        ArgumentException.ThrowIfNullOrWhiteSpace(text);

        var ch = await BotClient.GetChannelAsync(channel)
            ?? throw new InvalidOperationException($"Could not find a channel under id {channel} that this bot has access to");

        if (ch is not IMessageChannel messageChannel)
            throw new InvalidOperationException($"The channel under id {channel} is not a message channel");
        else
        {
            var (embed, components) = await BuildEmbedFromKeyboard(text, kr);

            return attachments is not null && attachments.Any()
                ? (long)(await messageChannel.SendFilesAsync(attachments.Select(a => new FileAttachment(
                    a.GetContent(),
                    a.AttachmentTitle ?? "notitle",
                    a.Description,
                    a.IsSpoiler,
                    a.IsThumbnail,
                    a.Duration
                )), text, messageReference: reply, flags: flags, embed: embed, components: components)).Id
                : (long)(await messageChannel.SendMessageAsync(text, messageReference: reply, flags: flags, embed: embed, components: components)).Id;
        }
    }

    public async Task AnswerKeyboardResponse(Guid conversationId, KeyboardResponse keyboardResponse, string? alertMessage, bool showAlert, int cacheTime = 0)
    {
        if (keyboardResponse.AttachedData is SocketMessageComponent comp)
            await comp.RespondAsync(alertMessage, ephemeral: !showAlert);
        else
            throw new ArgumentException("The response does not contain a valid SocketMessageComponent", nameof(keyboardResponse));
    }

    public async Task EditMessage(Guid conversationId, long messageId, string? newText, Keyboard? newKeyboard = default, MessageOptions options = default)
    {
        conversationId.UnpackDiscordConversationId(out var guild, out var channel);

        ArgumentException.ThrowIfNullOrWhiteSpace(newText);

        var ch = await BotClient.GetChannelAsync(channel)
            ?? throw new InvalidOperationException($"Could not find a channel under id {channel} that this bot has access to");

        if (ch is not IMessageChannel messageChannel)
            throw new InvalidOperationException($"The channel under id {channel} is not a message channel");

        var (embed, components) = await BuildEmbedFromKeyboard(newText, newKeyboard);

        await messageChannel.ModifyMessageAsync((ulong)messageId, f =>
        {
            f.Content = newText;
            if (newKeyboard is not null)
            {
                f.Embed = embed;
                f.Components = components;
            }
        });
    }

    public async Task DeleteMessage(Guid conversationId, long messageId)
    {
        conversationId.UnpackDiscordConversationId(out var guild, out var channel);

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
        => text.Equals(BotMention, StringComparison.OrdinalIgnoreCase)
        || text.Equals($"@{BotHandle}", StringComparison.OrdinalIgnoreCase)
        || text.Equals($"<@{DiscordBotId}>", StringComparison.OrdinalIgnoreCase);

    public bool ContainsReferenceToBot(string text)
        => text.Contains(BotMention, StringComparison.OrdinalIgnoreCase)
        || text.Contains($"@{BotHandle}", StringComparison.OrdinalIgnoreCase)
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
        SendWithoutNotification = true,
        ResponseMessages = true,
        UserInfoInMessage = true,
        DisciplinaryActionReasons = true
    };

    public async Task KickUser(Guid conversationId, long userId, string? reason = null)
    {
        conversationId.UnpackDiscordConversationId(out var guildId, out _);
        var guild = await BotClient.GetGuildAsync(guildId);
        var user = await guild.GetUserAsync((ulong)userId);
        await user.KickAsync(reason);
    }

    public async Task BanUser(Guid conversationId, long userId, int prune = 0, string? reason = null)
    {
        conversationId.UnpackDiscordConversationId(out var guildId, out _);
        var guild = await BotClient.GetGuildAsync(guildId);
        var user = await guild.GetUserAsync((ulong)userId);
        await user.BanAsync(prune, reason);
    }

    public async Task MuteUser(Guid conversationId, long userId, string? reason = null)
    {
        conversationId.UnpackDiscordConversationId(out var guildId, out _);
        var guild = await BotClient.GetGuildAsync(guildId);
        var user = await guild.GetUserAsync((ulong)userId);

        var muteRole = guild.Roles.FirstOrDefault(r => r.Name == "Muted");
        muteRole ??= await guild.CreateRoleAsync("Muted", new GuildPermissions(sendMessages: false), isMentionable: false);

        await user.AddRoleAsync(muteRole);
    }

    public async Task UnmuteUser(Guid conversationId, long userId, string? reason = null)
    {
        conversationId.UnpackDiscordConversationId(out var guildId, out _);
        var guild = await BotClient.GetGuildAsync(guildId);
        var user = await guild.GetUserAsync((ulong)userId);

        var muteRole = guild.Roles.FirstOrDefault(r => r.Name == "Muted");
        muteRole ??= await guild.CreateRoleAsync("Muted", new GuildPermissions(sendMessages: false), isMentionable: false);

        await user.RemoveRoleAsync(muteRole);
    }

    public async Task UnbanUser(Guid conversationId, long userId, string? reason = null)
    {
        conversationId.UnpackDiscordConversationId(out var guildId, out _);
        var guild = await BotClient.GetGuildAsync(guildId);
        var user = await guild.GetUserAsync((ulong)userId);
        await guild.RemoveBanAsync(user);
    }

    public async Task<long?> RespondToUpdate(UpdateContext context, string? text, Keyboard? keyboard = null, IEnumerable<MessageAttachment>? attachments = null, MessageOptions options = default)
    {
        if (context is DiscordInteractionUpdateContext interactionContext
         && interactionContext.Interaction is SocketCommandBase cmd)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(text);

            var (embed, components) = await BuildEmbedFromKeyboard(text, keyboard);

            if (attachments is not null && attachments.Any())
            {
                await cmd.RespondWithFilesAsync(attachments.Select(a => new FileAttachment(
                    a.GetContent(),
                    a.AttachmentTitle ?? "notitle",
                    a.Description,
                    a.IsSpoiler,
                    a.IsThumbnail,
                    a.Duration
                )), text, embed: embed, components: components);
            }
            else
                await cmd.RespondAsync(text, embed: embed, components: components);

            return null;
        }
        else if (context is DiscordComponentUpdateContext componentContext
         && componentContext.Interaction is SocketMessageComponent comp)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(text);

            var (embed, components) = await BuildEmbedFromKeyboard(text, keyboard);

            if (attachments is not null && attachments.Any())
            {
                await comp.RespondWithFilesAsync(attachments.Select(a => new FileAttachment(
                    a.GetContent(),
                    a.AttachmentTitle ?? "notitle",
                    a.Description,
                    a.IsSpoiler,
                    a.IsThumbnail,
                    a.Duration
                )), text, embed: embed, components: components);
            }
            else
                await comp.RespondAsync(text, embed: embed, components: components);

            return null;
        }

        await SendMessage(context.ConversationId, text, keyboard, context.Message?.MessageId, attachments, options);

        return null;
    }

    public BotStatusSet StatusCollection { get; }

    public readonly record struct KeyboardEmbed(Embed? Embed, MessageComponent? Component);
    public static async ValueTask<KeyboardEmbed> BuildEmbedFromKeyboard(string title, Keyboard? kr)
    {
        if (kr is Keyboard keyboard && keyboard != default)
        {
            var embedBuilder = new EmbedBuilder().WithTitle(title);
            var componentBuilder = new ComponentBuilder();

            foreach (var rowInfo in keyboard.Rows)
            {
                var row = new ActionRowBuilder();

                foreach (var keyInfo in rowInfo.Keys)
                {
                    ButtonBuilder button = new ButtonBuilder()
                        .WithLabel(keyInfo.Text)
                        .WithStyle(ButtonStyle.Primary)
                        .WithEmote(null)
                        .WithCustomId(keyInfo.Data)
                        .WithUrl(null)
                        .WithDisabled(false);

                    if (keyInfo.KeyDecorator is Func<object, ValueTask> keyDecorator)
                        await keyDecorator.Invoke(button);

                    row.WithButton(button);
                }

                if (rowInfo.RowDecorator is Func<object, ValueTask> rowDecorator)
                    await rowDecorator.Invoke(row);

                componentBuilder.AddRow(row);
            }

            if (keyboard.KeyboardDecorator is Func<object, ValueTask> decorator)
                await decorator.Invoke(new DiscordEmbedBuilderSet(embedBuilder, componentBuilder));

            return new(embedBuilder.Build(), componentBuilder.Build());
        }

        return default;
    }
}

internal static class DiscordBotStatuses
{
    public static BotStatusSet CreateStatusCollection(DiscordChatBotClient client)
        => new()
        {
            Typing = new DiscordDefaultBotStatus(client),
            SendingFile = null,
            SendingImage = null,
            SendingVideo = null,
            SendingVoiceMessage = null
        };

    public class DiscordDefaultBotStatus(DiscordChatBotClient client) : BotStatus(client)
    {
        public override async Task<IDisposable> SetStatus(Guid conversationId)
        {
            conversationId.UnpackDiscordConversationId(out var guild, out var channel);
            var ch = (ITextChannel)await client.BotClient.GetChannelAsync(channel);
            return ch.EnterTypingState();
        }
    }
}