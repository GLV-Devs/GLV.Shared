using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;
using System.Collections.Frozen;
using System.Diagnostics;

namespace GLV.Shared.ChatBot;

public partial class ChatBotManager
{
    public delegate ValueTask<TimeSpan> OnConversationContextThreadContentionHandler(UpdateContext update, int attempt);
    public delegate ValueTask OnUpdateTypeFilteredOutHandler(UpdateContext update);

    public ChatBotManager(
        Type defaultAction, 
        IEnumerable<ConversationActionDefinition> actions,
        ServiceDescriptor conversationStoreServiceDescription,
        string? chatBotManagerIdentifier = null,
        Func<UpdateContext, ValueTask<bool>>? updateFilter = null,
        IServiceCollection? configureServices = null
    )
    {
        ArgumentNullException.ThrowIfNull(conversationStoreServiceDescription);

        if (conversationStoreServiceDescription.ServiceType != typeof(IConversationStore))
            throw new ArgumentException("The store service descriptor doesn't describe a service type of IConversationStore. The implementation type needs only be assignable to it, but the service type MUST be IConversationStore", nameof(conversationStoreServiceDescription));

        UpdateFilter = updateFilter;
        Identifier = string.IsNullOrWhiteSpace(chatBotManagerIdentifier) ? Guid.NewGuid().ToString() : chatBotManagerIdentifier;

        DefaultAction = defaultAction.IsAssignableTo(typeof(ConversationActionBase))
        ? defaultAction
        : throw new ArgumentException($"The type {defaultAction.Name} submitted as default action is not a sub-class of ConversationActionBase", nameof(defaultAction));

        var serviceCollection = configureServices ?? new ServiceCollection();

        Actions = actions.Select(ActionProcessor).ToFrozenDictionary();
        Commands = actions.Where(x => string.IsNullOrWhiteSpace(x.CommandTrigger) is false)
                          .Select(CommandProcessor)
                          .ToFrozenDictionary();

        serviceCollection.Add(conversationStoreServiceDescription);

        KeyValuePair<string, ConversationCommandDefinition> CommandProcessor(ConversationActionDefinition x)
        {
            if (x.ConversationAction.IsAssignableTo(typeof(ConversationActionBase)) is false)
                throw new InvalidDataException($"The type {x.ConversationAction.Name} submitted as command {x.CommandTrigger} is not a sub-class of ConversationActionBase");

            serviceCollection.AddKeyedTransient(x.ConversationAction, CoreComposeCommandKey(x.ActionName));
            return new(x.CommandTrigger!, new ConversationCommandDefinition(x.ConversationAction, x.CommandTrigger!, x.CommandDescription));
        }

        KeyValuePair<string, Type> ActionProcessor(ConversationActionDefinition x)
        {
            if (x.ConversationAction.IsAssignableTo(typeof(ConversationActionBase)) is false)
                throw new InvalidDataException($"The type {x.ConversationAction.Name} submitted as action {x.ActionName} is not a sub-class of ConversationActionBase");

            serviceCollection.AddKeyedTransient(x.ConversationAction, CoreComposeServiceKey(x.ActionName));
            return new(x.ActionName, x.ConversationAction);
        }

        DefaultActionServiceKey = $"ConversationAction!{Identifier}::default";
        serviceCollection.AddKeyedScoped(defaultAction, DefaultActionServiceKey);
        ChatBotServices = serviceCollection.BuildServiceProvider();
    }

    /// <summary>
    /// Fired when an attempt is made to fetch a conversation store, but it's under thread contention (used by another thread, most likely the processing of another update in the same conversation)
    /// </summary>
    /// <remarks>
    /// Return a TimeSpan greater than 0 to denote the amount of time to wait before trying again. On some platforms under certain configurations, anything between 0 and 15 milliseconds will result in waiting roughly 15 milliseconds.
    /// </remarks>
    public event OnConversationContextThreadContentionHandler? OnConversationContextThreadContention;

    public event OnUpdateTypeFilteredOutHandler? OnUpdateTypeFilteredOut;

    protected Func<UpdateContext, ValueTask<bool>>? UpdateFilter { get; }

    public string Identifier { get; }
    
    public IServiceProvider ChatBotServices { get; }

    public FrozenDictionary<string, Type> Actions { get; }

    public FrozenDictionary<string, ConversationCommandDefinition> Commands { get; }

    public Type DefaultAction { get; }

    private string CoreComposeCommandKey(string trigger)
        => $"ConversationCommand!{Identifier}::{trigger}";

    private string CoreComposeServiceKey(string action)
        => $"ConversationAction!{Identifier}::{action}";

    public string DefaultActionServiceKey { get; } 

    protected virtual ValueTask<ConversationContext> CreateContext(UpdateContext update)
        => ValueTask.FromResult(new ConversationContext(update.ConversationId));

    private ConversationActionBase GetConversationAction(IServiceProvider services, string activeAction, Type type)
        => (ConversationActionBase)((IKeyedServiceProvider)services).GetKeyedService(type, ComposeServiceKey(activeAction))!;

    private ConversationActionBase GetDefaultConversationAction(IServiceProvider services, Type type)
        => (ConversationActionBase)((IKeyedServiceProvider)services).GetKeyedService(type, DefaultActionServiceKey)!;

    public virtual async ValueTask ConfigureChatBot(IChatBotClient client)
    {
        await client.SetBotCommands(Commands.Values);
    }

    public string ComposeCommandKey(string trigger)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(trigger);
        return Commands.ContainsKey(trigger) is false
            ? throw new ArgumentException($"Unknown ConversationCommand: '{trigger}'", nameof(trigger))
            : CoreComposeCommandKey(trigger);
    }

    public string ComposeServiceKey(string action)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(action);
        return Actions.ContainsKey(action) is false
            ? throw new ArgumentException($"Unknown ConversationAction: '{action}'", nameof(action))
            : CoreComposeServiceKey(action);
    }

    private static InvalidOperationException UnprocessableUpdateException(Guid conversationId, int attempt)
        => new($"An update of the conversation of id '{conversationId}' cannot be processed, as the system cannot continue waiting for it after attempt #{attempt}");

    protected virtual ValueTask<bool> FilterUpdates(UpdateContext update)
        => UpdateFilter?.Invoke(update) ?? ValueTask.FromResult(true);

    public virtual async Task SubmitUpdate(UpdateContext update)
    {
        using var scope = ChatBotServices.CreateScope();
        var services = scope.ServiceProvider;

        ConversationContext context;
        int attempt = 0;
        var store = services.GetRequiredService<IConversationStore>();
        while (true)
        {
            var fetchResult = await store.FetchConversation(update.ConversationId);
            if (fetchResult.NotObtainedReason is IConversationStore.ConversationNotObtainedReason.ConversationNotFound)
            {
                context = await CreateContext(update);
                await store.SaveChanges(context);
            }
            else if (fetchResult.NotObtainedReason is IConversationStore.ConversationNotObtainedReason.ConversationUnderThreadContention)
            {
                Debug.WriteLine($"Conversation of id '{update.ConversationId}' is under contention");
                var ta = OnConversationContextThreadContention?.Invoke(update, attempt++);
                if (ta is not ValueTask<TimeSpan> task)
                {
                    await Task.Delay(500);
                    continue;
                }

                var to = await task;
                if (to is TimeSpan timeout and { Ticks: > 0 })
                {
                    await Task.Delay(timeout);
                    continue;
                }
                throw UnprocessableUpdateException(update.ConversationId, attempt);
            }
            else
            {
                Debug.Assert(
                    fetchResult.NotObtainedReason is IConversationStore.ConversationNotObtainedReason.ConversationWasObtained,
                    "Reached a point in the code where the only possible NotObtainedReason was that it was obtained, but it did not equal this value"
                );
                Debug.Assert(
                    fetchResult.Context is not null,
                    "fetchResult.Context was unexpectedly null"
                );
                context = fetchResult.Context;
                break;
            }
        }

        ConversationActionBase action 
            = string.IsNullOrWhiteSpace(context.ActiveAction)
            ? GetDefaultConversationAction(services, DefaultAction)
                : Actions.TryGetValue(context.ActiveAction, out var type)
            ? GetConversationAction(services, context.ActiveAction, type)
                : throw new InvalidDataException($"Could not find an action by the name of '{context.ActiveAction}'");

        Debug.Assert(action is not null);

        await action.PerformActions(store, context, update, this);
    }
}
