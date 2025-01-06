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
    public ConversationContext Context
    {
        get => field ?? throw new InvalidOperationException("Context property is only available whilst performing an action");
        private set;
    }

    /// <summary>
    /// The Update Context surrounding the current action
    /// </summary>
    /// <remarks>
    /// This property is only accesible whilst performing an action (i.e. inside <see cref="PerformAsync"/>). And will throw an exception if an attempt is made to access it otherwise
    /// </remarks>
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
    public IScopedChatBotClient Bot
    {
        get => field ?? throw new InvalidOperationException("Context property is only available whilst performing an action");
        private set;
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
                await Bot.RespondWithText(notificationText);
            return true;
        }
        return false;
    }

    internal async Task<ConversationActionEndingKind> PerformActions(IServiceProvider services, IConversationStore store, ConversationContext context, UpdateContext update, ChatBotManager manager, IScopedChatBotClient client)
    {
        Debug.Assert(context is not null);
        Debug.Assert(update is not null);
        Debug.Assert(manager is not null);

        Context = context;
        Bot = client;
        ChatBotManager = manager;
        ConversationStore = store;
        Services = services;
        Update = update;
        try
        {
            return await PerformAsync(update);
        }
        finally
        {
            Context = null!;
            Bot = null!;
            ChatBotManager = null!;
            ConversationStore = null!;
            Update = null!;
        }
    }

    protected abstract Task<ConversationActionEndingKind> PerformAsync(UpdateContext update);
}
