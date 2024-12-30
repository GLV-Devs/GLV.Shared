using GLV.Shared.Common;
using System.Runtime.InteropServices;
using System.Text.Json.Serialization;

namespace GLV.Shared.ChatBot;

/// <summary>
/// Information regarding a conversation the bot is having with an user, service or another bot
/// </summary>
/// <remarks>
/// When inheriting from this class, be mindful of serialization! Most <see cref="IConversationStore"/> implementations serialize the context to store it. Note that <see cref="Step"/> and <see cref="ActiveAction"/> need special care, as they are set to ignore since they have <see langword="private setter"/>s
/// </remarks>
public class ConversationContext(Guid conversationId, Dictionary<string, object>? data = null)
{
    public Guid ConversationId { get; private set; } = conversationId;

    public Dictionary<string, object> Data { get; init; } = data ?? [];

    /// <summary>
    /// The step within <see cref="ActiveAction"/> the conversation is in
    /// </summary>
    /// <remarks>
    /// This property is decorated with <see cref="JsonIgnoreAttribute"/>, and needs special care when serializing due to its <see langword="private setter"/>
    /// </remarks>
    [JsonIgnore]
    public long Step { get; private set; }

    /// <summary>
    /// The current action the conversation is taking
    /// </summary>
    /// <remarks>
    /// This property is decorated with <see cref="JsonIgnoreAttribute"/>, and needs special care when serializing due to its <see langword="private setter"/>
    /// </remarks>
    [JsonIgnore]
    public string? ActiveAction { get; private set; }

    /// <summary>
    /// Sets the state of the conversation
    /// </summary>
    /// <remarks>
    /// This is meant to be used only within an action, and calling it outside of one is Undefined Behaviour
    /// </remarks>
    /// <param name="step">The action specific step the conversation is at</param>
    /// <param name="activeAction">The action that is currently being taken in this conversation, or <see langword="null"/> for the default action</param>
    /// <param name="manager">The manager that is currently executing the action setting the state. Do NOT store this manager in the class!</param>
    public virtual void SetState(long step, string? activeAction)
    {
        Step = step;
        ActiveAction = activeAction;
    }
}
