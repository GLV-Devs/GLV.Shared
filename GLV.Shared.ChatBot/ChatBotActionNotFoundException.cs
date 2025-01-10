namespace GLV.Shared.ChatBot;

[Serializable]
public class ChatBotActionNotFoundException : Exception
{
    public string ActionName { get; }

    public ChatBotActionNotFoundException(string actionName) : base(ComposeMessage(actionName)) { ActionName = actionName; }
    public ChatBotActionNotFoundException(string actionName, Exception inner) : base(ComposeMessage(actionName), inner) { ActionName = actionName; }

    protected ChatBotActionNotFoundException(
      System.Runtime.Serialization.SerializationInfo info,
      System.Runtime.Serialization.StreamingContext context) : base(info, context) { }

    private static string ComposeMessage(string actionName)
        => $"Could not find a ChatBotAction by the name of {actionName}";
}
