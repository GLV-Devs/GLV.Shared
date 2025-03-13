using GLV.Shared.ChatBot.Pipeline;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace GLV.Shared.ChatBot;

/// <summary>
/// Represents the base action of a conversation
/// </summary>
/// <remarks>
/// Derived classes should only be used through a <see cref="IServiceProvider"/> capable of constructing a new object, these are meant to be transient
/// </remarks>
public abstract class ConversationActionBase
{
    /// <summary>
    /// The Conversation Context surrounding the current action
    /// </summary>
    /// <remarks>
    /// This property is only accesible whilst performing an action (i.e. inside <see cref="PerformAsync"/>). And will throw an exception if an attempt is made to access it otherwise
    /// </remarks>
    [field: AllowNull]
    public ConversationContext Context
    {
        get => field ?? throw new InvalidOperationException("Context property is only available whilst performing an action");
        private set;
    }

    /// <summary>
    /// The ActionName of the ConversationAction
    /// </summary>
    public string? ActionName { get; private set; }

    /// <summary>
    /// The Update Context surrounding the current action
    /// </summary>
    /// <remarks>
    /// This property is only accesible whilst performing an action (i.e. inside <see cref="PerformAsync"/>). And will throw an exception if an attempt is made to access it otherwise
    /// </remarks>
    [field: AllowNull]
    public UpdateContext Update
    {
        get => field ?? throw new InvalidOperationException("Update property is only available whilst performing an action");
        private set;
    }

    /// <summary>
    /// The ChatBotManager available during this action
    /// </summary>
    /// <remarks>
    /// This property is only accesible whilst performing an action (i.e. inside <see cref="PerformAsync"/>). And will throw an exception if an attempt is made to access it otherwise
    /// </remarks>
    [field: AllowNull]
    public ChatBotManager ChatBotManager
    {
        get => field ?? throw new InvalidOperationException("ChatBotManager property is only available whilst performing an action");
        private set;
    }

    /// <summary>
    /// The Services available during this action
    /// </summary>
    /// <remarks>
    /// This property is only accesible whilst performing an action (i.e. inside <see cref="PerformAsync"/>). And will throw an exception if an attempt is made to access it otherwise
    /// </remarks>
    [field: AllowNull]
    public IServiceProvider Services
    {
        get => field ?? throw new InvalidOperationException("Services property is only available whilst performing an action");
        private set;
    }

    /// <summary>
    /// The ConversationStore available during this action
    /// </summary>
    /// <remarks>
    /// This property is only accesible whilst performing an action (i.e. inside <see cref="PerformAsync"/>). And will throw an exception if an attempt is made to access it otherwise
    /// </remarks>
    [field: AllowNull]
    public IConversationStore ConversationStore
    {
        get => field ?? throw new InvalidOperationException("ConversationStore property is only available whilst performing an action");
        private set;
    }

    /// <summary>
    /// The BotClient available during this action
    /// </summary>
    /// <remarks>
    /// This property is only accesible whilst performing an action (i.e. inside <see cref="PerformAsync"/>). And will throw an exception if an attempt is made to access it otherwise
    /// </remarks>
    [field: AllowNull]
    public IScopedChatBotClient Bot
    {
        get => field ?? throw new InvalidOperationException("Context property is only available whilst performing an action");
        private set;
    }

    private PipelineHandlerCollection? Pipeline;

    public void SinkLogMessage(int logLevel, string message, int eventId, Exception? exception)
        => ChatBotManager.SinkLogMessage(logLevel, message, eventId, exception, Services);

    public async Task<bool> ExecuteActionPipeline(bool excludeGlobalHandlers = false)
    {
        Debug.Assert(Pipeline is not null);

        var pipelineContext = Update.pipelineContext ??= new PipelineContext(this);
        pipelineContext.Handled = false;

        if (Update.Message is Message message && await Pipeline.ExecuteMessageHandlerLine(ActionName, message, pipelineContext))
            return true;

        if (Update.KeyboardResponse is KeyboardResponse kr && await Pipeline.ExecuteKeyboardHandlerLine(ActionName, kr, pipelineContext))
            return true;
        
        return false;
    }

    /// <summary>
    /// Checks if the user is attempting to cancel the current action
    /// </summary>
    /// <remarks>
    /// It's safe to take no action other than ending the current action if this method returns <see langword="true"/>, but only if <paramref name="setContextState"/> is set to <see langword="true"/>
    /// </remarks>
    /// <param name="setContextState">If <see langword="true"/>, the conversation context will be reset</param>
    /// <param name="notificationText">The text to send to the user in response to the cancellation. <see langword="null"/> if nothing is to be said</param>
    /// <returns></returns>
    public async ValueTask<bool> CheckForCancellation(bool setContextState = true, string? notificationText = "The action has been cancelled. What else can I do for you?")
    {
        if (ChatBotManager.CheckForCancellation(Update, Context, setContextState))
        {
            if (string.IsNullOrWhiteSpace(notificationText) is false)
                await Bot.SendMessage(notificationText);
            return true;
        }
        return false;
    }

    internal async Task PerformActions(
        IServiceProvider services, 
        IConversationStore store, 
        ConversationContext context, 
        UpdateContext update, 
        ChatBotManager manager, 
        IScopedChatBotClient client,
        PipelineHandlerCollection pipeline,
        string? actionName
    )
    {
        Debug.Assert(actionName is null || string.IsNullOrWhiteSpace(actionName) is false);
        Debug.Assert(context is not null);
        Debug.Assert(update is not null);
        Debug.Assert(manager is not null);
        Debug.Assert(pipeline is not null);
        Debug.Assert(client is not null);
        Debug.Assert(store is not null);
        Debug.Assert(services is not null);

        Context = context;
        Bot = client;
        ChatBotManager = manager;
        ConversationStore = store;
        Services = services;
        Update = update;
        Pipeline = pipeline;
        try
        {
            await PerformAsync(update);
        }
        finally
        {
            Context = null!;
            Bot = null!;
            ChatBotManager = null!;
            ConversationStore = null!;
            Services = null!;
            Update = null!;
            Pipeline = null!;
        }
    }

    protected abstract Task PerformAsync(UpdateContext update);
}
