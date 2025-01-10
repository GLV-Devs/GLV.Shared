using System.Collections.Frozen;
using System.Collections.Immutable;

namespace GLV.Shared.ChatBot.Pipeline;

public interface IChatBotPipelineHandler : IChatBotPipelineKeyboardHandler, IChatBotPipelineMessageHandler
{

}

public interface IChatBotPipelineMessageHandler
{
    public Task TryProcessMessage(PipelineContext context, Message message);
}

public interface IChatBotPipelineKeyboardHandler
{
    public Task TryProcessKeyboardResponse(PipelineContext context, KeyboardResponse keyboard);
}
