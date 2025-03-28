using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace GLV.Shared.ChatBot.Internal;

public class StepMethodInfo(MethodInfo method)
{
    public MethodInfo Method { get; } = method ?? throw new ArgumentNullException(nameof(method));

    public Task<StepMethodReturn> Invoke(ConversationActionBase action)
        => (Task<StepMethodReturn>)Method.Invoke(action, null)!;
}
