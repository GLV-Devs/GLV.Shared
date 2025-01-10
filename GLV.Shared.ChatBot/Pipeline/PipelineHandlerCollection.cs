using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Collections;
using System.Collections.Frozen;
using System.Collections.Immutable;

namespace GLV.Shared.ChatBot.Pipeline;

public sealed class PipelineHandlerCollection : IEnumerable<Type>
{
    private readonly FrozenSet<Type> allHandlers;

    public const string PipelineHandlerServiceKey = "!!GLV.Shared.ChatBot::pipeline-handler!!";

    public static PipelineHandlerCollection Empty { get; } = new();

    private PipelineHandlerCollection()
    {
        allHandlers = FrozenSet<Type>.Empty;
    }

    public PipelineHandlerCollection(IEnumerable<Type> types, IServiceCollection services, string? actionName)
    {
        HashSet<Type> messageHandlersBuffer = [];
        HashSet<Type> keyboardHandlersBuffer = [];

        foreach (var type in types)
        {
            bool valid = false;

            if (type.IsAssignableTo(typeof(IChatBotPipelineMessageHandler)))
            {
                messageHandlersBuffer.Add(type);
                services.TryAddKeyedScoped(type, PipelineHandlerServiceKey);
                valid = true;
            }

            if (type.IsAssignableTo(typeof(IChatBotPipelineKeyboardHandler)))
            {
                keyboardHandlersBuffer.Add(type);
                services.TryAddKeyedScoped(type, PipelineHandlerServiceKey);
                valid = true;
            }

            if (valid is false)
                throw new ArgumentException($"The type included in the handlers list '{type.AssemblyQualifiedName}' is not a valid chatbot pipeline handler. It needs to implement at least one of the following interfaces: 'IChatBotPipelineMessageHandler', or 'IChatBotPipelineKeyboardHandler'", nameof(types));
        }

        allHandlers = types.ToFrozenSet();
        MessageHandlers = [.. messageHandlersBuffer];
        KeyboardHandlers = [.. keyboardHandlersBuffer];
    }

    public ImmutableArray<Type> MessageHandlers { get; }
    public ImmutableArray<Type> KeyboardHandlers { get; }

    internal async Task<bool> ExecuteMessageHandlerLine(string? actionName, Message message, PipelineContext context)
    {
        var services = context.Services;
        for (int i = 0; i < MessageHandlers.Length; i++)
        {
            var messageHandlerType = MessageHandlers[i];
            var handler = (IChatBotPipelineMessageHandler)services.GetRequiredKeyedService(messageHandlerType, PipelineHandlerServiceKey);
            context.AddProcessorPass(messageHandlerType);
            await handler.TryProcessMessage(context, message);
            if (context.Handled)
                return true;
        }

        return false; // This will only be hit if no handler set the property 'Handled' to true. As it's always immediately checked after processing
    }

    internal async Task<bool> ExecuteKeyboardHandlerLine(string? actionName, KeyboardResponse keyboardResponse, PipelineContext context)
    {
        var services = context.Services;
        for (int i = 0; i < KeyboardHandlers.Length; i++)
        {
            var messageHandlerType = KeyboardHandlers[i];
            var handler = (IChatBotPipelineKeyboardHandler)services.GetRequiredKeyedService(messageHandlerType, PipelineHandlerServiceKey);
            context.AddProcessorPass(messageHandlerType);
            await handler.TryProcessKeyboardResponse(context, keyboardResponse);
            if (context.Handled)
                return true;
        }

        return false; // This will only be hit if no handler set the property 'Handled' to true. As it's always immediately checked after processing
    }

    public IEnumerator<Type> GetEnumerator()
        => allHandlers.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
        => allHandlers.GetEnumerator();
}