using System.Collections.Concurrent;
using System.Collections.Frozen;
using GLV.Shared.EntityFramework;
using Serilog.Core;
using Serilog.Events;

namespace GLV.Shared.Server.API.Logging;

public delegate IDatabaseExecutionLogEntry ExecutionLogEntryFactory(LogEvent @event);

public class DatabaseLogSink : ILogEventSink
{
    public LogEventLevel Level { get; set; }

    private readonly Func<ConcurrentQueue<IDatabaseExecutionLogEntry>?> QueueProvider;
    private readonly FrozenSet<IExceptionDumper>? ExceptionDumpers;
    private readonly ExecutionLogEntryFactory LogEntryFactory;

    public DatabaseLogSink(int buffering, Func<ConcurrentQueue<IDatabaseExecutionLogEntry>?> queueProvider, IEnumerable<IExceptionDumper>? dumpers, ExecutionLogEntryFactory logEntryFactory)
    {
        ArgumentNullException.ThrowIfNull(logEntryFactory);
        ArgumentNullException.ThrowIfNull(queueProvider);

        if (buffering <= 0)
            throw new ArgumentOutOfRangeException(nameof(buffering), "The buffering index cannot be less than or equal to 0");

        LogEntryFactory = logEntryFactory;
        QueueProvider = queueProvider;

        if (dumpers is not null)
        {
            var set = dumpers.ToFrozenSet();
            if (set.Count is > 0)
                ExceptionDumpers = set;
        }
    }

    public void Emit(LogEvent logEvent)
    {
        ConcurrentQueue<IDatabaseExecutionLogEntry>? queue;
        if (logEvent is null || (queue = QueueProvider()) is null) return;

        var ev = LogEntryFactory(logEvent);

        if (logEvent.Exception is Exception e && ExceptionDumpers is not null)
        {
            Guid id = Guid.NewGuid();

            foreach (var dumper in ExceptionDumpers)
                dumper.WriteExceptionDump(e, id, null);

            ev.ExceptionDumpId = id;
        }

        queue.Enqueue(ev);
    }
}
