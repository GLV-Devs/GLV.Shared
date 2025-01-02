namespace GLV.Shared.ChatBot;

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public class ConversationActionAttribute : Attribute
{
    public ConversationActionAttribute(string? actionName, string? commandTrigger = null, string? commandDescription = null)
    {
        ActionName = actionName;
        IsDefaultAction = false;
        CommandTrigger = commandTrigger;
        CommandDescription = commandDescription;
    }

    public ConversationActionAttribute(bool isDefaultAction)
    {
        ActionName = null;
        IsDefaultAction = isDefaultAction;
    }

    public bool IsDefaultAction { get; }
    public string? ActionName { get; }
    public string? CommandTrigger { get; }
    public string? CommandDescription { get; }
}
