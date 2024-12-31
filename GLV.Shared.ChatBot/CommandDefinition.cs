namespace GLV.Shared.ChatBot;

public readonly record struct ConversationCommandDefinition(
    Type ConversationAction,
    string CommandTrigger,
    string? CommandDescription
)
{
    public string CommandTrigger { get; } = string.IsNullOrWhiteSpace(CommandTrigger) is false
                                          ? CommandTrigger
                                          : throw new ArgumentException("CommandTrigger cannot be null or whitespace", nameof(CommandTrigger));
}

public readonly record struct ConversationActionDefinition(
    Type ConversationAction,
    string? ActionName = null,
    string? CommandTrigger = null,
    string? CommandDescription = null
)
{
    public string ActionName { get; } = ActionName ?? ConversationAction.Name;
}
