using GLV.Shared.ChatBot.Converters;
using GLV.Shared.Common;
using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GLV.Shared.ChatBot;

internal sealed class ActualContextData<T> : TypedContextData<T>
{
    public readonly record struct ValueBuffer(T? Value);
    public readonly record struct Buffer(string AssemblyQualifiedDataTypeName, ValueBuffer Data);

    internal override void SerializeBuffer(Utf8JsonWriter writer, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, new Buffer(AssemblyQualifiedDataTypeName, new(Value)), options);
    }

    [JsonPropertyOrder(0)]
    public string AssemblyQualifiedDataTypeName => typeof(T).AssemblyQualifiedName!;
}

public abstract class TypedContextData<T> : ContextData
{
    internal TypedContextData() { }

    [JsonPropertyOrder(1)]
    public T? Value { get; set; }
}

[JsonConverter(typeof(ContextDataJsonConverter))]
public abstract class ContextData
{
    internal ContextData() { }

    internal abstract void SerializeBuffer(Utf8JsonWriter writer, JsonSerializerOptions options);

    public static JsonConverter<ContextData> JsonConverter { get; } = new ContextDataJsonConverter();
}

/// <summary>
/// Information regarding a conversation the bot is having with an user, service or another bot
/// </summary>
/// <remarks>
/// When inheriting from this class, be mindful of serialization! Most <see cref="IConversationStore"/> implementations serialize the context to store it. Note that <see cref="Step"/> and <see cref="ActiveAction"/> need special care, as they are set to ignore since they have <see langword="private setter"/>s
/// </remarks>
public class ConversationContext(Guid conversationId, ContextDataSet? data = null)
{
    public Guid ConversationId { get; private set; } = conversationId;

    public ContextDataSet Data { get; init; } = data ?? new();

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

    /// <summary>
    /// A shorthand for <see cref="SetState(long, string?)"/> with arguments "<paramref name="step"/>, <see cref="ActiveAction"/>"
    /// </summary>
    /// <param name="step">The action specific step the conversation is at</param>
    public void SetStep(long step)
        => SetState(step, ActiveAction);

    /// <summary>
    /// A shorthand for <see cref="SetState(long, string?)"/> with arguments "<c>0</c>, <see langword="null"/>"
    /// </summary>
    public void ResetState()
        => SetState(0, null);
}
