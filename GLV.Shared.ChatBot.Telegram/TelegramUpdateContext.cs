using GLV.Shared.ChatBot;
using System.Text.Json.Serialization;
using Telegram.Bot.Types.Enums;
using WTelegram.Types;

namespace GLV.Shared.ChatBot.Telegram;

public class TelegramUpdateContext(
    Update update,
    IChatBotClient client,
    Guid? conversationId = null,
    bool isHandledByBotClient = false
) : UpdateContext(client, conversationId ?? GetConversationId(update, client), TelegramPlatform)
{
    public Update Update { get; } = update;

    public override bool IsHandledByBotClient => isHandledByBotClient;

    public static Guid GetConversationId(Update update, IChatBotClient client)
        => update.GetTelegramConversationId(client.BotId);

    public override Message? Message { get; }
        = update.Message is null
        ? null
        : new Message(
            update.Message.Text, 
            update.Message.Id,
            update.Message.GetMessageReference(),
            update.Message.From?.GetUserInfo(),
            update.Message.Type != MessageType.Text,
            update.Message
        );

    public override KeyboardResponse? KeyboardResponse { get; }
        = update.CallbackQuery is null
        ? null
        : new KeyboardResponse(update.CallbackQuery.Id, update.CallbackQuery.Data);

    public override MemberEvent? MemberEvent { get; }
        = update.ChatMember is null
        ? null
        : new MemberEvent(
            update.ChatMember.NewChatMember.User.GetUserInfo(),
            update.ChatMember.From?.GetUserInfo(),
            update.ChatMember.NewChatMember.Status switch
            { 
                ChatMemberStatus.Left => MemberEventKind.MemberLeft, 
                ChatMemberStatus.Kicked => MemberEventKind.MemberKicked, 
                _ => update.ChatMember.OldChatMember?.IsInChat is false ? MemberEventKind.MemberJoined : MemberEventKind.Other,
            }
        );

    public override bool IsDirectMessage { get; }
        = update.Message is not null && update.Message.Chat.Type is ChatType.Private
       || update.CallbackQuery is not null && update.CallbackQuery.Message is not null && update.CallbackQuery.Message.Chat.Type is ChatType.Private;
}
