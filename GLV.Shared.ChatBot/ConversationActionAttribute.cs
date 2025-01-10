using System.Reflection;

namespace GLV.Shared.ChatBot;

[AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = true)]
public class ConversationActionPipelineHandlerAttribute(Type pipelineHandler) : Attribute
{
    public Type PipelineHandler { get; } = pipelineHandler;
}

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public class ConversationActionAttribute : Attribute
{
    public readonly struct ActionInfoDetails(Type type)
    {
        public readonly Type Type
            = type;

        public readonly ConversationActionAttribute? ConversationActionAttribute
            = type.GetCustomAttribute<ConversationActionAttribute>();

        public readonly IEnumerable<ConversationActionPipelineHandlerAttribute> ConversationActionPipelineHandlerAttributes
            = type.GetCustomAttributes<ConversationActionPipelineHandlerAttribute>();
    }

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
