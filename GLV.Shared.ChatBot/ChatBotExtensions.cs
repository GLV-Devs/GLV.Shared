namespace GLV.Shared.ChatBot;

public static class ChatBotExtensions
{
    public static async Task<bool> TryDeleteMessage(this IChatBotClient client, Guid conversationId, long messageId)
    {
        try
        {
            await client.DeleteMessage(conversationId, messageId);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static async Task<bool> TryDeleteMessage(this IScopedChatBotClient client, long messageId)
    {
        try
        {
            await client.DeleteMessage(messageId);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
