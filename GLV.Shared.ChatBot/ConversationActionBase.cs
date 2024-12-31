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
    public IChatBotClient BotClient
    {
        get => field ?? throw new InvalidOperationException("Context property is only available whilst performing an action");
        private set;
    }

    internal async Task PerformActions(IConversationStore store, ConversationContext context, UpdateContext update, ChatBotManager manager)
    {
        Debug.Assert(context is not null);
        Debug.Assert(update is not null);
        Debug.Assert(manager is not null);

        Context = context;
        BotClient = update.Client;
        ChatBotManager = manager;
        ConversationStore = store;
        try
        {
            await PerformAsync(update);
            await store.SaveChanges(Context);
        }
        finally
        {
            Context = null!;
            BotClient = null!;
            ChatBotManager = null!;
            ConversationStore = null!;
        }
    }

    protected abstract Task PerformAsync(UpdateContext update);
}
