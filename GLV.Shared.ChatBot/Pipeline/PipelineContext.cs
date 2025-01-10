using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GLV.Shared.ChatBot.Pipeline;

public sealed record class PipelineContext(ConversationActionBase ActiveAction)
{
    private readonly List<Type> processedBy = [];
    public IEnumerable<Type> ProcessedBy => processedBy;

    public UpdateContext Update => ActiveAction.Update;
    public ConversationContext Context => ActiveAction.Context;
    public IScopedChatBotClient Bot => ActiveAction.Bot;
    public IServiceProvider Services => ActiveAction.Services ;

    internal void AddProcessorPass(Type type)
        => processedBy.Add(type);

    public bool Handled { get; set; } 
}
