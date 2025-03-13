using GLV.Shared.ChatBot.Pipeline;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Text;

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
    public delegate ValueTask OnUpdateExceptionThrownHandler(
        Exception exc, 
        IScopedChatBotClient client, 
        IServiceProvider services
    );

    private OnUpdateExceptionThrownHandler? exceptionHandler;

    public ChatBotManager(
        DefaultActionDefinition defaultAction,
        IEnumerable<ConversationActionDefinition> actions,
        IEnumerable<Type> globalPipelineHandlers,
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

        var serviceCollection = configureServices ?? new ServiceCollection();

        serviceCollection.Add(conversationStoreServiceDescription);

        DefaultActionServiceKey = $"ConversationAction!{Identifier}::default";
        serviceCollection.AddKeyedScoped(defaultAction.ConversationAction, DefaultActionServiceKey);

        GlobalPipelineHandlers = globalPipelineHandlers?.Any() is not true
            ? PipelineHandlerCollection.Empty
            : new PipelineHandlerCollection(globalPipelineHandlers, serviceCollection, null);

        DefaultAction = new ConversationActionInformation(
            defaultAction.ConversationAction.IsAssignableTo(typeof(ConversationActionBase))
            ? defaultAction.ConversationAction
            : throw new ArgumentException($"The type {defaultAction.ConversationAction.Name} submitted as default action is not a sub-class of ConversationActionBase", nameof(defaultAction)),
            null,
            null,
            null,
            defaultAction.LocalPipelineHandlers,
            serviceCollection
        );

        Actions = actions.Select(ActionProcessor).ToFrozenDictionary();
        Commands = actions.Where(x => x.ValidateCommandTrigger())
                          .Select(CommandProcessor)
                          .ToFrozenDictionary();

        ChatBotServices = serviceCollection.BuildServiceProvider();

        // -- Local functions

        KeyValuePair<string, ConversationActionInformation> CommandProcessor(ConversationActionDefinition x)
        {
            Debug.Assert(Actions.ContainsKey(x.ActionName));

            return x.ConversationAction.IsAssignableTo(typeof(ConversationActionBase)) is false
                        ? throw new InvalidDataException($"The type {x.ConversationAction.Name} submitted as command {x.CommandTrigger} is not a sub-class of ConversationActionBase")
                        : new(
                            x.CommandTrigger!,
                            Actions[x.ActionName]
                        );
        }

        KeyValuePair<string, ConversationActionInformation> ActionProcessor(ConversationActionDefinition x)
        {
            if (x.ConversationAction.IsAssignableTo(typeof(ConversationActionBase)) is false)
                throw new InvalidDataException($"The type {x.ConversationAction.Name} submitted as action {x.ActionName} is not a sub-class of ConversationActionBase");

            serviceCollection.AddKeyedTransient(x.ConversationAction, CoreComposeServiceKey(x.ActionName));
            return new(
                    x.ActionName!,
                    new ConversationActionInformation(
                        x.ConversationAction,
                        x.ActionName,
                        x.CommandTrigger,
                        x.CommandDescription,
                        x.LocalPipelineHandlers,
                        serviceCollection
                    )
                );
        }
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

    public PipelineHandlerCollection GlobalPipelineHandlers { get; }

    public FrozenDictionary<string, ConversationActionInformation> Actions { get; }
    public FrozenDictionary<string, ConversationActionInformation> Commands { get; }

    public SinkLogMessageHandler? SinkLogMessageAction { get; set; }

    public void SinkLogMessage(
        int logLevel,
        string message,
        int eventId,
        Exception? exception,
        IServiceProvider? services = null
    )
        => SinkLogMessageAction?.Invoke(logLevel, message, eventId, exception, services ?? ChatBotServices);

    public ConversationActionInformation DefaultAction { get; }

    public virtual IScopedChatBotClient ScopeClient(IChatBotClient client, Guid conversation)
        => new ScopedChatBotClient(conversation, client);

    private string CoreComposeServiceKey(string action)
        => $"ConversationAction!{Identifier}::{action}";

    public string DefaultActionServiceKey { get; } 

    protected virtual ValueTask<ConversationContext> CreateContext(UpdateContext update)
        => ValueTask.FromResult(new ConversationContext(update.ConversationId));

    private ConversationActionBase GetConversationAction(IServiceProvider services, string activeAction, Type type)
        => (ConversationActionBase)((IKeyedServiceProvider)services).GetKeyedService(type, ComposeServiceKey(activeAction))!;

    private ConversationActionBase GetDefaultConversationAction(IServiceProvider services, ConversationActionInformation info)
        => (ConversationActionBase)((IKeyedServiceProvider)services).GetKeyedService(info.ConversationAction, DefaultActionServiceKey)!;

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
    public ValueTask<ConversationActionInformation?> CheckIfCommand(UpdateContext update)
        => CoreCheckForAction(update, Commands);

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

    public virtual ValueTask<ConversationActionInformation?> CoreCheckForAction(
        UpdateContext update,
        IDictionary<string, ConversationActionInformation> set
    )
    {
        if (update.Message is Message msg && string.IsNullOrWhiteSpace(msg.Text) is false)
        {
            int index = msg.Text.IndexOf(' ');
            var cmdText = index > 0 ? msg.Text[..index] : msg.Text;
            if (update.Client.IsValidBotCommand(cmdText, out var cmd) && Commands.TryGetValue(cmd, out var definition)) 
            {
                return ValueTask.FromResult<ConversationActionInformation?>(definition);
            }
        }

        return ValueTask.FromResult<ConversationActionInformation?>(null);
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

    public Func<FrozenDictionary<string, ConversationActionInformation>, string>? HelpStringComposer { get; set; }

    public string HelpString => field ??= ComposeHelpString(Commands);

    protected virtual string ComposeHelpString(FrozenDictionary<string, ConversationActionInformation> dict)
    {
        if (HelpStringComposer is Func<FrozenDictionary<string, ConversationActionInformation>, string> composer)
            return composer.Invoke(dict);

        StringBuilder sb = new(100 * dict.Count);
        sb.AppendLine("The commands available to me are: \n");
        foreach (var (k, v) in dict)
        {
            sb.Append("\t - ").Append(k);
            if (string.IsNullOrWhiteSpace(v.CommandDescription) is false)
                sb.Append(" : ").Append(v.CommandDescription);

            sb.AppendLine();
        }

        sb.AppendLine("\nIf you want me to send a picture of Ari, just send 'Ari' to the chat! (without the quotes)");

        return sb.ToString();
    }

    public virtual async Task SubmitUpdate(UpdateContext update, ConversationContext? context = null)
    {
        using var scope = ChatBotServices.CreateScope();
        var services = scope.ServiceProvider;

        var store = services.GetRequiredService<IConversationStore>();

        context ??= await GetOrCreateContext(store, update);

        if (string.IsNullOrWhiteSpace(update.JumpToActiveAction) is false)
        {
            if (Actions.ContainsKey(update.JumpToActiveAction))
                context.SetState(update.JumpToActiveActionStep ?? 0, update.JumpToActiveAction);
        }

        var client = ScopeClient(update.Client, context.ConversationId);

        if (update.IsHandledByBotClient)
            await client.ProcessUpdate(update, context);

        try
        {
            while (true && update.IsHandledByBotClient is false) 
            {
                ConversationActionInformation? actionInfo = null;
                ConversationActionBase action 
                    = string.IsNullOrWhiteSpace(context.ActiveAction)
                    ? GetDefaultConversationAction(services, DefaultAction)
                    : Actions.TryGetValue(context.ActiveAction, out actionInfo)
                        ? GetConversationAction(services, context.ActiveAction, actionInfo.ConversationAction)
                        : throw new ChatBotActionNotFoundException($"Could not find an action by the name of '{context.ActiveAction}'");

                actionInfo ??= DefaultAction;
                Debug.Assert(action is not null);

                try
                {
                    await action.PerformActions(
                        scope.ServiceProvider, 
                        store, 
                        context, 
                        update, 
                        this, 
                        client,
                        actionInfo.Pipeline,
                        actionInfo.ActionName
                    );
                }
                catch(Exception exc)
                {
                    var t = exceptionHandler?.Invoke(exc, client, scope.ServiceProvider);
                    if (t is ValueTask task)
                    {
                        await task;
                        SinkLogMessageAction?.Invoke(3, "An exception was thrown while trying to perform an action", -12355522, exc, services);
                        break;
                    }

                    throw;
                }

                break;
            }
        }
        catch(ChatBotActionNotFoundException excp)
        {
            SinkLogMessageAction?.Invoke(3, "Could not find an action that was attempted to be performed", -1350155522, excp, services);
            var t = exceptionHandler?.Invoke(excp, client, scope.ServiceProvider);
            if (t is ValueTask task)
                await task;
            context.ResetState();
        }
        catch(Exception excp)
        {
            var t = exceptionHandler?.Invoke(excp, client, scope.ServiceProvider);
            if (t is ValueTask task)
            {
                await task;
                SinkLogMessageAction?.Invoke(4, "An exception was thrown while trying to perform an action", -1352155522, excp, services);
            }
            else
                throw;
        }
        finally
        {
            try
            {
                await store.SaveChanges(context);
            }
            catch(Exception e)
            {
                SinkLogMessageAction?.Invoke(4, "An unexpected error ocurred while trying to save the context", -232433452, e, services);
            }
        }
    }
}
