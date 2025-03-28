namespace GLV.Shared.ChatBot;

public static class BotStatusExtensions
{
    private sealed class NotActuallyDisposableButAlsoDisposableSingleton : IDisposable
    {
        private NotActuallyDisposableButAlsoDisposableSingleton() { }

        public static IDisposable Instance { get; } = new NotActuallyDisposableButAlsoDisposableSingleton();
        public static Task<IDisposable> CompletedTaskInstance { get; } = Task.FromResult(Instance);

        public void Dispose() { }
    }

    public static Task<IDisposable> TrySetStatus(this BotStatus? status, Guid conversationId)
    {
        var t = status?.SetStatus(conversationId);
        return t is not null ? t : NotActuallyDisposableButAlsoDisposableSingleton.CompletedTaskInstance;
    }

    public static Task<IDisposable> TrySetStatus(this ScopedBotStatus status)
    {
        var t = status.SetStatus();
        return t is not null ? t : NotActuallyDisposableButAlsoDisposableSingleton.CompletedTaskInstance;
    }
}
