using System.Collections.Concurrent;
using Telegram.Bot.Types;

namespace GLV.Shared.ChatBot.Telegram;

/// <summary>
/// Does the same thing as <see cref="TelegramBotReactor"/>, except that this one queues the updates so that the submission can be marshalled to another, user defined call
/// </summary>
public class TaskMarshallingTelegramBotReactor(
    TelegramChatBotClient bot, 
    ChatBotManager manager, 
    Func<Update, IChatBotClient, Guid>? conversationIdFactory = null
) : TelegramBotReactor(bot, manager, conversationIdFactory)
{
    private readonly ConcurrentQueue<UpdateContext> updates = [];

    protected override Task Client_OnUpdate(global::Telegram.Bot.Update arg)
    {
        updates.Enqueue(new TelegramUpdateContext(arg, Client, ConversationIdFactory));
        return Task.CompletedTask;
    }

    public async ValueTask<bool> SubmitNextUpdate()
    {
        if (updates.TryDequeue(out var update))
        {
            await Manager.SubmitUpdate(update);
            return true;
        }

        return false;
    }

    public async Task<int> SubmitAllUpdates()
    {
        int updateCount = 0;
        while (await SubmitNextUpdate()) updateCount++;
        return updateCount;
    }
}
