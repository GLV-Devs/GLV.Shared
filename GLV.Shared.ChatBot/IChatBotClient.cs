namespace GLV.Shared.ChatBot;

public interface IChatBotClient
{
    public string BotId { get; }
    public object UnderlyingBotClientObject { get; }
}
