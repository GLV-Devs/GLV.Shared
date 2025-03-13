using Discord;
using Discord.Commands;
using Discord.WebSocket;
using GLV.Shared.ChatBot.Discord.DiscordUpdates;
using System.Diagnostics;
using System.Net;
using System.Threading.Channels;

namespace GLV.Shared.ChatBot.Discord;

public class SocketDiscordChatBotClient(
    string botId,
    DiscordSocketClient client,
    ChatBotManager manager,
    CommandService commandService,
    DiscordBotCredentials credentials,
    Func<DiscordUpdateContext, ChatBotManager, Task>? updateHandler = null
) : DiscordChatBotClient(
    botId,
    client,
    manager,
    commandService,
    updateHandler
)
{
    public DiscordSocketClient SocketClient => (DiscordSocketClient)BotClient;

    public override async Task PrepareBot()
    {
        if (credentials is null || credentials.BotToken is null)
            throw new InvalidOperationException("Invalid credentials");

        await SocketClient.LoginAsync(TokenType.Bot, credentials.BotToken);

        SocketClient.ChannelCreated += React_ChannelCreated;
        SocketClient.ChannelDestroyed += React_ChannelDestroyed;
        SocketClient.ChannelUpdated += React_ChannelUpdated;
        //SocketClient.VoiceChannelStatusUpdated += React_VoiceChannelStatusUpdated;
        SocketClient.MessageReceived += React_MessageReceived;
        //SocketClient.MessageDeleted += React_MessageDeleted;
        //SocketClient.MessagesBulkDeleted += React_MessagesBulkDeleted;
        //SocketClient.MessageUpdated += React_MessageUpdated;
        //SocketClient.ReactionAdded += React_ReactionAdded;
        //SocketClient.ReactionRemoved += React_ReactionRemoved;
        //SocketClient.ReactionsCleared += React_ReactionsCleared;
        //SocketClient.ReactionsRemovedForEmote += React_ReactionsRemovedForEmote;
        //SocketClient.RoleCreated += React_RoleCreated;
        //SocketClient.RoleDeleted += React_RoleDeleted;
        //SocketClient.RoleUpdated += React_RoleUpdated;
        //SocketClient.JoinedGuild += React_JoinedGuild;
        //SocketClient.LeftGuild += React_LeftGuild;
        //SocketClient.GuildAvailable += React_GuildAvailable;
        //SocketClient.GuildUnavailable += React_GuildUnavailable;
        //SocketClient.GuildMembersDownloaded += React_GuildMembersDownloaded;
        //SocketClient.GuildUpdated += React_GuildUpdated;
        //SocketClient.GuildJoinRequestDeleted += React_GuildJoinRequestDeleted;
        //SocketClient.GuildScheduledEventCreated += React_GuildScheduledEventCreated;
        //SocketClient.GuildScheduledEventUpdated += React_GuildScheduledEventUpdated;
        //SocketClient.GuildScheduledEventCancelled += React_GuildScheduledEventCancelled;
        //SocketClient.GuildScheduledEventCompleted += React_GuildScheduledEventCompleted;
        //SocketClient.GuildScheduledEventStarted += React_GuildScheduledEventStarted;
        //SocketClient.GuildScheduledEventUserAdd += React_GuildScheduledEventUserAdd;
        //SocketClient.GuildScheduledEventUserRemove += React_GuildScheduledEventUserRemove;
        //SocketClient.IntegrationCreated += React_IntegrationCreated;
        //SocketClient.IntegrationUpdated += React_IntegrationUpdated;
        //SocketClient.IntegrationDeleted += React_IntegrationDeleted;
        //SocketClient.UserJoined += React_UserJoined;
        //SocketClient.UserLeft += React_UserLeft;
        //SocketClient.UserBanned += React_UserBanned;
        //SocketClient.UserUnbanned += React_UserUnbanned;
        //SocketClient.UserUpdated += React_UserUpdated;
        //SocketClient.GuildMemberUpdated += React_GuildMemberUpdated;
        //SocketClient.UserVoiceStateUpdated += React_UserVoiceStateUpdated;
        //SocketClient.VoiceServerUpdated += React_VoiceServerUpdated;
        //SocketClient.CurrentUserUpdated += React_CurrentUserUpdated;
        //SocketClient.UserIsTyping += React_UserIsTyping;
        //SocketClient.RecipientAdded += React_RecipientAdded;
        //SocketClient.RecipientRemoved += React_RecipientRemoved;
        //SocketClient.PresenceUpdated += React_PresenceUpdated;
        //SocketClient.InviteCreated += React_InviteCreated;
        //SocketClient.InviteDeleted += React_InviteDeleted;
        //SocketClient.InteractionCreated += React_InteractionCreated;
        //SocketClient.ButtonExecuted += React_ButtonExecuted;
        //SocketClient.SelectMenuExecuted += React_SelectMenuExecuted;
        //SocketClient.SlashCommandExecuted += React_SlashCommandExecuted;
        //SocketClient.UserCommandExecuted += React_UserCommandExecuted;
        //SocketClient.MessageCommandExecuted += React_MessageCommandExecuted;
        //SocketClient.AutocompleteExecuted += React_AutocompleteExecuted;
        //SocketClient.ModalSubmitted += React_ModalSubmitted;
        //SocketClient.ApplicationCommandCreated += React_ApplicationCommandCreated;
        //SocketClient.ApplicationCommandUpdated += React_ApplicationCommandUpdated;
        //SocketClient.ApplicationCommandDeleted += React_ApplicationCommandDeleted;
        //SocketClient.ThreadCreated += React_ThreadCreated;
        //SocketClient.ThreadUpdated += React_ThreadUpdated;
        //SocketClient.ThreadDeleted += React_ThreadDeleted;
        //SocketClient.ThreadMemberJoined += React_ThreadMemberJoined;
        //SocketClient.ThreadMemberLeft += React_ThreadMemberLeft;
        //SocketClient.StageStarted += React_StageStarted;
        //SocketClient.StageEnded += React_StageEnded;
        //SocketClient.StageUpdated += React_StageUpdated;
        //SocketClient.RequestToSpeak += React_RequestToSpeak;
        //SocketClient.SpeakerAdded += React_SpeakerAdded;
        //SocketClient.SpeakerRemoved += React_SpeakerRemoved;
        //SocketClient.GuildStickerCreated += React_GuildStickerCreated;
        //SocketClient.GuildStickerUpdated += React_GuildStickerUpdated;
        //SocketClient.GuildStickerDeleted += React_GuildStickerDeleted;
        //SocketClient.WebhooksUpdated += React_WebhooksUpdated;
        //SocketClient.AuditLogCreated += React_AuditLogCreated;
        //SocketClient.AutoModRuleCreated += React_AutoModRuleCreated;
        //SocketClient.AutoModRuleUpdated += React_AutoModRuleUpdated;
        //SocketClient.AutoModRuleDeleted += React_AutoModRuleDeleted;
        //SocketClient.AutoModActionExecuted += React_AutoModActionExecuted;
        //SocketClient.EntitlementCreated += React_EntitlementCreated;
        //SocketClient.EntitlementUpdated += React_EntitlementUpdated;
        //SocketClient.EntitlementDeleted += React_EntitlementDeleted;
        //SocketClient.SubscriptionCreated += React_SubscriptionCreated;
        //SocketClient.SubscriptionUpdated += React_SubscriptionUpdated;
        //SocketClient.SubscriptionDeleted += React_SubscriptionDeleted;

        sem = new(1, 1);
        await SocketClient.StartAsync();
        sem.Wait();
        SocketClient.Connected += SocketClient_Connected;
        sem.Wait();
        await base.PrepareBot();
    }

    private Task SocketClient_Connected()
    {
        Debug.Assert(sem is not null);
        sem.Release();
        return Task.CompletedTask;
    }

    private SemaphoreSlim? sem;

    private Task React_ChannelCreated(SocketChannel channel)
        => SubmitUpdate(
            new DiscordChannelUpdateContext(this, channel, channel.PackDiscordConversationId(), DiscordUpdateKind.ChannelCreated, false)
        );

    private Task React_ChannelDestroyed(SocketChannel channel)
        => SubmitUpdate(
            new DiscordChannelUpdateContext(this, channel, channel.PackDiscordConversationId(), DiscordUpdateKind.ChannelDeleted, false)
        );

    private Task React_ChannelUpdated(SocketChannel old, SocketChannel @new)
        => SubmitUpdate(
            new DiscordChannelUpdatedUpdateContext(this, old, @new, @new.PackDiscordConversationId(), false)
        );

    private Task React_VoiceChannelStatusUpdated()
        => Task.CompletedTask;

    private Task React_MessageReceived(SocketMessage msg) 
        => SubmitUpdate(
            new DiscordMessageReceivedUpdateContext(this, msg, msg.Channel.PackDiscordConversationId(), false)
        );

    private Task React_MessageDeleted()
        => Task.CompletedTask;
    
    private Task React_MessagesBulkDeleted()
        => Task.CompletedTask;
    
    private Task React_MessageUpdated()
        => Task.CompletedTask;
    
    private Task React_ReactionAdded()
        => Task.CompletedTask;
    
    private Task React_ReactionRemoved()
        => Task.CompletedTask;
    
    private Task React_ReactionsCleared()
        => Task.CompletedTask;
    
    private Task React_ReactionsRemovedForEmote()
        => Task.CompletedTask;
    
    private Task React_RoleCreated()
        => Task.CompletedTask;
    
    private Task React_RoleDeleted()
        => Task.CompletedTask;
    
    private Task React_RoleUpdated()
        => Task.CompletedTask;
    
    private Task React_JoinedGuild()
        => Task.CompletedTask;
    
    private Task React_LeftGuild()
        => Task.CompletedTask;
    
    private Task React_GuildAvailable()
        => Task.CompletedTask;
    
    private Task React_GuildUnavailable()
        => Task.CompletedTask;
    
    private Task React_GuildMembersDownloaded()
        => Task.CompletedTask;
    
    private Task React_GuildUpdated()
        => Task.CompletedTask;
    
    private Task React_GuildJoinRequestDeleted()
        => Task.CompletedTask;
    
    private Task React_GuildScheduledEventCreated()
        => Task.CompletedTask;
    
    private Task React_GuildScheduledEventUpdated()
        => Task.CompletedTask;
    
    private Task React_GuildScheduledEventCancelled()
        => Task.CompletedTask;
    
    private Task React_GuildScheduledEventCompleted()
        => Task.CompletedTask;
    
    private Task React_GuildScheduledEventStarted()
        => Task.CompletedTask;
    
    private Task React_GuildScheduledEventUserAdd()
        => Task.CompletedTask;
    
    private Task React_GuildScheduledEventUserRemove()
        => Task.CompletedTask;
    
    private Task React_IntegrationCreated()
        => Task.CompletedTask;
    
    private Task React_IntegrationUpdated()
        => Task.CompletedTask;
    
    private Task React_IntegrationDeleted()
        => Task.CompletedTask;
    
    private Task React_UserJoined()
        => Task.CompletedTask;
    
    private Task React_UserLeft()
        => Task.CompletedTask;
    
    private Task React_UserBanned()
        => Task.CompletedTask;
    
    private Task React_UserUnbanned()
        => Task.CompletedTask;
    
    private Task React_UserUpdated()
        => Task.CompletedTask;
    
    private Task React_GuildMemberUpdated()
        => Task.CompletedTask;
    
    private Task React_UserVoiceStateUpdated()
        => Task.CompletedTask;
    
    private Task React_VoiceServerUpdated()
        => Task.CompletedTask;
    
    private Task React_CurrentUserUpdated()
        => Task.CompletedTask;
    
    private Task React_UserIsTyping()
        => Task.CompletedTask;
    
    private Task React_RecipientAdded()
        => Task.CompletedTask;
    
    private Task React_RecipientRemoved()
        => Task.CompletedTask;
    
    private Task React_PresenceUpdated()
        => Task.CompletedTask;
    
    private Task React_InviteCreated()
        => Task.CompletedTask;
    
    private Task React_InviteDeleted()
        => Task.CompletedTask;
    
    private Task React_InteractionCreated()
        => Task.CompletedTask;
    
    private Task React_ButtonExecuted()
        => Task.CompletedTask;
    
    private Task React_SelectMenuExecuted()
        => Task.CompletedTask;
    
    private Task React_SlashCommandExecuted()
        => Task.CompletedTask;
    
    private Task React_UserCommandExecuted()
        => Task.CompletedTask;
    
    private Task React_MessageCommandExecuted()
        => Task.CompletedTask;
    
    private Task React_AutocompleteExecuted()
        => Task.CompletedTask;
    
    private Task React_ModalSubmitted()
        => Task.CompletedTask;
    
    private Task React_ApplicationCommandCreated()
        => Task.CompletedTask;
    
    private Task React_ApplicationCommandUpdated()
        => Task.CompletedTask;
    
    private Task React_ApplicationCommandDeleted()
        => Task.CompletedTask;
    
    private Task React_ThreadCreated()
        => Task.CompletedTask;
    
    private Task React_ThreadUpdated()
        => Task.CompletedTask;
    
    private Task React_ThreadDeleted()
        => Task.CompletedTask;
    
    private Task React_ThreadMemberJoined()
        => Task.CompletedTask;
    
    private Task React_ThreadMemberLeft()
        => Task.CompletedTask;
    
    private Task React_StageStarted()
        => Task.CompletedTask;
    
    private Task React_StageEnded()
        => Task.CompletedTask;
    
    private Task React_StageUpdated()
        => Task.CompletedTask;
    
    private Task React_RequestToSpeak()
        => Task.CompletedTask;
    
    private Task React_SpeakerAdded()
        => Task.CompletedTask;
    
    private Task React_SpeakerRemoved()
        => Task.CompletedTask;
    
    private Task React_GuildStickerCreated()
        => Task.CompletedTask;
    
    private Task React_GuildStickerUpdated()
        => Task.CompletedTask;
    
    private Task React_GuildStickerDeleted()
        => Task.CompletedTask;
    
    private Task React_WebhooksUpdated()
        => Task.CompletedTask;
    
    private Task React_AuditLogCreated()
        => Task.CompletedTask;
    
    private Task React_AutoModRuleCreated()
        => Task.CompletedTask;
    
    private Task React_AutoModRuleUpdated()
        => Task.CompletedTask;
    
    private Task React_AutoModRuleDeleted()
        => Task.CompletedTask;
    
    private Task React_AutoModActionExecuted()
        => Task.CompletedTask;
    
    private Task React_EntitlementCreated()
        => Task.CompletedTask;
    
    private Task React_EntitlementUpdated()
        => Task.CompletedTask;
    
    private Task React_EntitlementDeleted()
        => Task.CompletedTask;
    
    private Task React_SubscriptionCreated()
        => Task.CompletedTask;
    
    private Task React_SubscriptionUpdated()
        => Task.CompletedTask;
    
    private Task React_SubscriptionDeleted()
        => Task.CompletedTask;
}
