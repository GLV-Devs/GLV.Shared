namespace GLV.Shared.Hosting;

public enum WorkQueueEntryStatus
{
    Failure = -2,
    ExceptionThrown = -1,
    NotStarted = 0,
    Started = 1,
    Completed = 2
}

public interface IWorkQueueItem
{
    public long Id { get; }
    public object? View { get; }
}
