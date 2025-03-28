namespace GLV.Shared.ChatBot;

public readonly record struct StepMethodReturn(bool ExecutePerformActions)
{
    public static implicit operator StepMethodReturn(bool executePerformActions)
        => new(executePerformActions);
}
