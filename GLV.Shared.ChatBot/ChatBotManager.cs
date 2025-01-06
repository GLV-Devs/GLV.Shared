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
    public delegate void SinkLogMessageHandler(
        int logLevel, 
        string message, 
        int eventId, 
        Exception? exception,
        IServiceProvider services
    );
    public delegate ValueTask<ConversationActionEndingKind?> OnUpdateExceptionThrownHandler(
        Exception exc, 
        IScopedChatBotClient client, 
        IServiceProvider services
    );

    private OnUpdateExceptionThrownHandler? exceptionHandler;

    public ChatBotManager(
        Type defaultAction,
        IEnumerable<ConversationActionDefinition> actions,
        ServiceDescriptor conversationStoreServiceDescription,
        string? chatBotManagerIdentifier = null,
        Func<UpdateContext, ValueTask<bool>>? updateFilter = null,
        IServiceCollection? configureServices = null,
        OnUpdateExceptionThrownHandler? exceptionHandler = null
    )
    {
        ArgumentNullException.ThrowIfNull(conversationStoreServiceDescription);

        if (conversationStoreServiceDescription.ServiceType != typeof(IConversationStore))
            throw new ArgumentException("The store service descriptor doesn't describe a service type of IConversationStore. The implementation type needs only be assignable to it, but the service type MUST be IConversationStore", nameof(conversationStoreServiceDescription));

        this.exceptionHandler = exceptionHandler;

        UpdateFilter = updateFilter;
        Identifier = string.IsNullOrWhiteSpace(chatBotManagerIdentifier) ? Guid.NewGuid().ToString() : chatBotManagerIdentifier;

        DefaultAction = defaultAction.IsAssignableTo(typeof(ConversationActionBase))
        ? defaultAction
        : throw new ArgumentException($"The type {defaultAction.Name} submitted as default action is not a sub-class of ConversationActionBase", nameof(defaultAction));

        var serviceCollection = configureServices ?? new ServiceCollection();

        Actions = actions.Select(ActionProcessor).ToFrozenDictionary();
        Commands = actions.Where(x => x.ValidateCommandTrigger())
                          .Select(CommandProcessor)
                          .ToFrozenDictionary();

        serviceCollection.Add(conversationStoreServiceDescription);

        KeyValuePair<string, ConversationActionDefinition> CommandProcessor(ConversationActionDefinition x) 
            => x.ConversationAction.IsAssignableTo(typeof(ConversationActionBase)) is false
                ? throw new InvalidDataException($"The type {x.ConversationAction.Name} submitted as command {x.CommandTrigger} is not a sub-class of ConversationActionBase")
                : new(x.CommandTrigger!, x);

        KeyValuePair<string, ConversationActionDefinition> ActionProcessor(ConversationActionDefinition x)
        {
            if (x.ConversationAction.IsAssignableTo(typeof(ConversationActionBase)) is false)
                throw new InvalidDataException($"The type {x.ConversationAction.Name} submitted as action {x.ActionName} is not a sub-class of ConversationActionBase");

            serviceCollection.AddKeyedTransient(x.ConversationAction, CoreComposeServiceKey(x.ActionName));
            return new(x.ActionName, x);
        }

        DefaultActionServiceKey = $"ConversationAction!{Identifier}::default";
        serviceCollection.AddKeyedScoped(defaultAction, DefaultActionServiceKey);
        ChatBotServices = serviceCollection.BuildServiceProvider(); // Dangerous?
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

    public FrozenDictionary<string, ConversationActionDefinition> Actions { get; }
    public FrozenDictionary<string, ConversationActionDefinition> Commands { get; }

    public SinkLogMessageHandler? SinkLogMessageAction { get; set; }

    public Type DefaultAction { get; }

    public virtual IScopedChatBotClient ScopeClient(IChatBotClient client, Guid conversation)
        => new ScopedChatBotClient(conversation, client);

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
        await client.PrepareBot();
        await client.SetBotCommands(Commands.Values);
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

    /// <summary>
    /// Attempts to process the inputted command. If it matches a command, sets the context to the current state, and returns <see langword="true"/>. Otherwise, leaves the context unchanged and returns <see langword="false"/>
    /// </summary>
    /// <remarks>
    /// To execute the command immediately without waiting for another message from the user, return <see cref="ConversationActionEndingKind.Repeat"/> immediately after this method returns <see langword="true"/>
    /// </remarks>
    /// <param name="update"></param>
    /// <param name="context"></param>
    /// <param name="setContextState">If <see langword="true"/>, then the context will be set to step 0 of the Conversation Action</param>
    public ValueTask<bool> CheckIfCommand(UpdateContext update, ConversationContext context, bool setContextState = true)
        => CoreCheckForAction(update, context, Commands, setContextState);

    /// <summary>
    /// Checks if the user is attempting to cancel the current action
    /// </summary>
    /// <param name="update"></param>
    /// <param name="context"></param>
    /// <param name="setContextState">If <see langword="true"/>, then the context will be reset automatically</param>
    public virtual bool CheckForCancellation(UpdateContext update, ConversationContext context, bool setContextState = true)
    {
        if (update.Message is Message msg && string.IsNullOrWhiteSpace(msg.Text) is false
            && update.Client.IsValidBotCommand(msg.Text, out var cmd) && string.Equals(cmd, "cancel", StringComparison.OrdinalIgnoreCase))
        {
            if (setContextState)
                context.ResetState();
            return true;
        }

        return false;
    }

    public virtual ValueTask<bool> CoreCheckForAction(
        UpdateContext update, 
        ConversationContext context,
        IDictionary<string, ConversationActionDefinition> set,
        bool setContextState
    )
    {
        if (update.Message is Message msg && string.IsNullOrWhiteSpace(msg.Text) is false)
        {
            int index = msg.Text.IndexOf(' ');
            var cmdText = index > 0 ? msg.Text[..index] : msg.Text;
            if(update.Client.IsValidBotCommand(cmdText, out var cmd) && Commands.TryGetValue(cmd, out var definition))
            {
                if(setContextState)
                    context.SetState(0, definition.ActionName);
                return ValueTask.FromResult(true);
            }
        }

        return ValueTask.FromResult(false);
    }

    // This method should also receive a set of commands to look through
    // TryProcessDefaultCommand checks only for help, cancel and stop.

    protected virtual async Task<ConversationContext> GetOrCreateContext(IConversationStore store, UpdateContext update)
    {
        int attempt = 0;
        while (true)
        {
            var fetchResult = await store.FetchConversation(update.ConversationId);
            if (fetchResult.NotObtainedReason is IConversationStore.ConversationNotObtainedReason.ConversationNotFound)
            {
                var context = await CreateContext(update);
                await store.SaveChanges(context);
                return context;
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
                return fetchResult.Context;
            }
        }
    }

    public virtual async Task SubmitUpdate(UpdateContext update, ConversationContext? context = null)
    {
        using var scope = ChatBotServices.CreateScope();
        var services = scope.ServiceProvider;

        var store = services.GetRequiredService<IConversationStore>();

        context ??= await GetOrCreateContext(store, update);
        var client = ScopeClient(update.Client, context.ConversationId);

        try
        {
            while (true) 
            {
                ConversationActionBase action 
                    = string.IsNullOrWhiteSpace(context.ActiveAction)
                    ? GetDefaultConversationAction(services, DefaultAction)
                        : Actions.TryGetValue(context.ActiveAction, out var actionDefinition)
                    ? GetConversationAction(services, context.ActiveAction, actionDefinition.ConversationAction)
                        : throw new InvalidDataException($"Could not find an action by the name of '{context.ActiveAction}'");

                Debug.Assert(action is not null);

                ConversationActionEndingKind ending;
                try
                {
                    ending = await action.PerformActions(scope.ServiceProvider, store, context, update, this, client);
                }
                catch(Exception exc)
                {
                    var t = exceptionHandler?.Invoke(exc, client, scope.ServiceProvider);
                    if (t is ValueTask<ConversationActionEndingKind?> task && await task is ConversationActionEndingKind end)
                    {
                        SinkLogMessageAction?.Invoke(2, "An exception was thrown while trying to perform an action", -12355522, exc, services);
                        if (end == ConversationActionEndingKind.Repeat)
                            continue;

                        break;
                    }

                    throw;
                }

                if (ending == ConversationActionEndingKind.Repeat)
                    continue;

                break;
            }
        }
        finally
        {
            try
            {
                await store.SaveChanges(context);
            }
            catch(Exception e)
            {
                SinkLogMessageAction?.Invoke(3, "An unexpected error ocurred while trying to save the context", -232433452, e, services);
            }
        }
    }
}
