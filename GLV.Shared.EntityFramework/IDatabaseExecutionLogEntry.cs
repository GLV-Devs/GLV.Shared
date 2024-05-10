using System.Diagnostics.CodeAnalysis;
using GLV.Shared.Data.Identifiers;
using Serilog.Events;

namespace GLV.Shared.EntityFramework;
public interface IDatabaseExecutionLogEntry
{
    Snowflake Id { get; set; }
    Guid? ExceptionDumpId { get; set; }
    string? ExceptionType { get; set; }
    LogEventLevel LogEventLevel { get; set; }
    [DisallowNull]
    string? Message { get; set; }
    string? TraceId { get; set; }
}