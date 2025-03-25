using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace GLV.Shared.ChatBot;
public partial class ChatBotManager
{
    private sealed class ConversationActionDefinitionEqualityComparer : IEqualityComparer<ConversationActionDefinition>
    {
        private ConversationActionDefinitionEqualityComparer() { }

        public static ConversationActionDefinitionEqualityComparer Comparer { get; } = new();

        public bool Equals(ConversationActionDefinition x, ConversationActionDefinition y)
            => x.ConversationAction == y.ConversationAction;

        public int GetHashCode([DisallowNull] ConversationActionDefinition obj)
            => obj.ConversationAction.GetHashCode();
    }

    public static (DefaultActionDefinition defaultAction, HashSet<ConversationActionDefinition> actions) GatherReflectedActions(
        Func<ConversationActionAttribute.ActionInfoDetails, bool>? predicate = null,
        IEnumerable<Assembly>? assemblies = null
    )
    {
        assemblies ??= AppDomain.CurrentDomain.GetAssemblies();
        var q = assemblies.SelectMany(x => x.GetTypes())
                          .Select(x => new ConversationActionAttribute.ActionInfoDetails(x))
                          .Where(x => x.ConversationActionAttribute is not null);

        if (predicate is not null)
            q = q.Where(predicate);

        DefaultActionDefinition? defaultAction = null;

        HashSet<ConversationActionDefinition> actionInfos = new(ConversationActionDefinitionEqualityComparer.Comparer);
        foreach (var action in q)
        {
            Debug.Assert(action.ConversationActionAttribute is not null);

            if (action.Type.IsAssignableTo(typeof(ConversationActionBase)) is false)
                throw new InvalidDataException($"The type {action.Type.Name} decorated as action {action.ConversationActionAttribute.ActionName} is not a sub-class of ConversationActionBase");

            if (action.ConversationActionAttribute.IsDefaultAction)
            {
                if (defaultAction is not null)
                    throw new InvalidDataException($"Found conflicting actions: {defaultAction.Value.ConversationAction.Name} and {action.Type.Name} are marked as the default action within the matching types");
                defaultAction = new DefaultActionDefinition(
                    action.Type, 
                    action.ConversationActionPipelineHandlerAttributes?.Select(x => x.PipelineHandler)
                );
            }
            else if (actionInfos.Add(new(
                action.Type,
                action.ConversationActionPipelineHandlerAttributes?.Select(x => x.PipelineHandler),
                action.ConversationActionAttribute.ActionName ?? action.Type.Name,
                action.ConversationActionAttribute.CommandTrigger,
                action.ConversationActionAttribute.CommandDescription
            )) is false)
                throw new InvalidDataException($"Found conflicting actions: More than one action bears the name of '{action.ConversationActionAttribute.ActionName}'");
        }

        if (defaultAction.HasValue is false)
            throw new InvalidDataException($"Could not locate a valid default action");

        return (defaultAction.Value, actionInfos);
    }

    public static ChatBotManager CreateChatBotWithReflectedActions(
        IEnumerable<Type> globalPipelineHandlers,
        string? chatBotManagerIdentifier = null,
        Func<ConversationActionAttribute.ActionInfoDetails, bool>? predicate = null,
        Func<UpdateContext, ValueTask<bool>>? updateFilter = null,
        IServiceCollection? configureServices = null,
        IEnumerable<Assembly>? assemblies = null,
        ChatBotManager.OnUpdateExceptionThrownHandler? exceptionHandler = null
    )
    {
        var (defaultAction, actions) = GatherReflectedActions(predicate, assemblies);
        return new ChatBotManager(
            defaultAction,
            actions,
            globalPipelineHandlers,
            chatBotManagerIdentifier,
            updateFilter,
            configureServices,
            exceptionHandler
        );
    }
}
