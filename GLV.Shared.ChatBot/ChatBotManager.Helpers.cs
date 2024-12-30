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
            => x.ActionType == y.ActionType;

        public int GetHashCode([DisallowNull] ActionInfo obj)
            => obj.ActionType.GetHashCode();
    }

    public readonly record struct ActionInfo(bool IsDefaultAction, string ActionName, Type ActionType);

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

        return q.Select(x => new ActionInfo(x.attr!.IsDefaultAction, x.attr.ActionName ?? x.acti.GetType().Name, x.acti));
    }

    public static ChatBotManager CreateChatBotWithReflectedActions(
        IConversationStore store,
        string? chatBotManagerIdentifier = null,
        Func<(ConversationActionAttribute Attribute, Type Type), bool>? predicate = null,
        IEnumerable<Assembly>? assemblies = null
    )
    {
        var actions = GatherReflectedActions(predicate, assemblies);
        ActionInfo? @default = null;
        HashSet<ActionInfo> actionInfos = new(ActionInfoEqualityComparer.Comparer);
        foreach (var action in actions)
        {
            if (action.ActionType.IsAssignableTo(typeof(ConversationActionBase)) is false)
                throw new InvalidDataException($"The type {action.ActionType.Name} decorated as action {action.ActionName} is not a sub-class of ConversationActionBase");

            if (action.IsDefaultAction)
            {
                if (@default is not null)
                    throw new InvalidDataException($"Found conflicting actions: {@default.Value.ActionName} and {action.ActionName} are marked as the default action within the matching types");
                @default = action;
            }
            else if (actionInfos.Add(action) is false)
                throw new InvalidDataException($"Found conflicting actions: More than one action bears the name of '{action.ActionName}'");
        }

        if (@default.HasValue is false)
            throw new InvalidDataException($"Could not locate a valid default action");

        return new ChatBotManager(
            @default.Value.ActionType, 
            actionInfos.Select(x => new KeyValuePair<string, Type>(x.ActionName, x.ActionType)),
            store,
            chatBotManagerIdentifier
        );
    }
}
