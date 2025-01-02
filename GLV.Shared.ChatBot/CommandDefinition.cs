namespace GLV.Shared.ChatBot;

public readonly record struct ConversationActionDefinition(
    Type ConversationAction,
    string? ActionName = null,
    string? CommandTrigger = null,
    string? CommandDescription = null
)
{
    internal bool ValidateCommandTrigger()
    {
        if (CommandTrigger is null)
            return false;

        if (string.IsNullOrWhiteSpace(CommandTrigger))
            throw new InvalidDataException("CommandTrigger cannot be only whitespace if it's not null");

        for (int i = 0; i < CommandTrigger.Length; i++)
            if (char.IsWhiteSpace(CommandTrigger[i]))
                throw new InvalidDataException("CommandTrigger cannot contain any whitespace");

        return true;
    }

    public string ActionName { get; } = ActionName ?? ConversationAction.Name;
}
