using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace GLV.Shared.ChatBot;
public partial class ChatBotManager
{
    private sealed class ActionInfoEqualityComparer : IEqualityComparer<ActionInfo>
    {
        private ActionInfoEqualityComparer() { }

        public static ActionInfoEqualityComparer Comparer { get; } = new();

        public bool Equals(ActionInfo x, ActionInfo y)
            => x.Definition.ConversationAction == y.Definition.ConversationAction;

        public int GetHashCode([DisallowNull] ActionInfo obj)
            => obj.Definition.ConversationAction.GetHashCode();
    }

    public readonly record struct ActionInfo(bool IsDefaultAction, ConversationActionDefinition Definition);

    public static IEnumerable<ActionInfo> GatherReflectedActions(
        Func<(ConversationActionAttribute Attribute, Type Type), bool>? predicate = null,
        IEnumerable<Assembly>? assemblies = null
    )
    {
        assemblies ??= AppDomain.CurrentDomain.GetAssemblies();
        var q = assemblies.SelectMany(x => x.GetTypes())
                          .Select(x => (attr: x.GetCustomAttribute<ConversationActionAttribute>()!, acti: x))
                          .Where(x => x.attr is not null);

        if (predicate is not null)
            q = q.Where(predicate);

        return q.Select(x => new ActionInfo(
            x.attr!.IsDefaultAction, 
            new(
                x.acti, 
                x.attr.ActionName ?? x.acti.GetType().Name,
                x.attr.CommandTrigger,
                x.attr.CommandDescription
            )
        ));
    }

    public static ChatBotManager CreateChatBotWithReflectedActions(
        ServiceDescriptor conversationStoreServiceDescription,
        string? chatBotManagerIdentifier = null,
        Func<(ConversationActionAttribute Attribute, Type Type), bool>? predicate = null,
        Func<UpdateContext, ValueTask<bool>>? updateFilter = null,
        IServiceCollection? configureServices = null,
        IEnumerable<Assembly>? assemblies = null
    )
    {
        if (conversationStoreServiceDescription.ServiceType != typeof(IConversationStore))
            throw new ArgumentException("The store service descriptor doesn't describe a service type of IConversationStore. The implementation type needs only be assignable to it, but the service type MUST be IConversationStore", nameof(conversationStoreServiceDescription));

        var actions = GatherReflectedActions(predicate, assemblies);
        ActionInfo? @default = null;
        HashSet<ActionInfo> actionInfos = new(ActionInfoEqualityComparer.Comparer);
        foreach (var action in actions)
        {
            if (action.Definition.ConversationAction.IsAssignableTo(typeof(ConversationActionBase)) is false)
                throw new InvalidDataException($"The type {action.Definition.ConversationAction.Name} decorated as action {action.Definition.ActionName} is not a sub-class of ConversationActionBase");

            if (action.IsDefaultAction)
            {
                if (@default is not null)
                    throw new InvalidDataException($"Found conflicting actions: {@default.Value.Definition.ActionName} and {action.Definition.ActionName} are marked as the default action within the matching types");
                @default = action;
            }
            else if (actionInfos.Add(action) is false)
                throw new InvalidDataException($"Found conflicting actions: More than one action bears the name of '{action.Definition.ActionName}'");
        }

        if (@default.HasValue is false)
            throw new InvalidDataException($"Could not locate a valid default action");

        return new ChatBotManager(
            @default.Value.Definition.ConversationAction, 
            actionInfos.Select(x => x.Definition),
            conversationStoreServiceDescription,
            chatBotManagerIdentifier,
            updateFilter,
            configureServices
        );
    }
}
