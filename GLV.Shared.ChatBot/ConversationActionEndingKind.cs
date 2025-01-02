namespace GLV.Shared.ChatBot;

public enum ConversationActionEndingKind
{
    /// <summary>
    /// Signals the <see cref="ChatBotManager"/> that no further action is required until the user sends another message
    /// </summary>
    Finished = 0,

    /// <summary>
    /// Signals the <see cref="ChatBotManager"/> that further action is required, and a new action should be executed with the held ConversationContext
    /// </summary>
    Repeat = 1,
}
