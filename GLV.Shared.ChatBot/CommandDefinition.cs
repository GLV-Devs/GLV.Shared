using GLV.Shared.ChatBot.Pipeline;
using Microsoft.Extensions.DependencyInjection;

namespace GLV.Shared.ChatBot;

public sealed class ConversationActionInformation
{
    internal ConversationActionInformation(
        Type conversationAction,
        string? actionName,
        string? commandTrigger,
        string? commandDescription,
        IEnumerable<Type>? localPipelineHandlers,
        IServiceCollection services
    )
    {
        ConversationAction = conversationAction;
        ActionName = actionName;
        CommandTrigger = commandTrigger;
        CommandDescription = commandDescription;

        Pipeline = localPipelineHandlers is null || localPipelineHandlers.Any() is false
            ? PipelineHandlerCollection.Empty
            : new PipelineHandlerCollection(localPipelineHandlers, services, ActionName);
    }

    public Type ConversationAction { get; }
    public string? ActionName { get; }
    public string? CommandTrigger { get; }
    public string? CommandDescription { get; }
    public PipelineHandlerCollection Pipeline { get; }
}

public readonly record struct DefaultActionDefinition(
    Type ConversationAction,
    IEnumerable<Type>? LocalPipelineHandlers
);

public readonly record struct ConversationActionDefinition(
    Type ConversationAction,
    IEnumerable<Type>? LocalPipelineHandlers,
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
