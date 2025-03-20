using System.Diagnostics.CodeAnalysis;
using GLV.Shared.Data.Identifiers;
using Microsoft.Extensions.Logging;

namespace GLV.Shared.EntityFramework;
public interface IDatabaseExecutionLogEntry
{
    long Id { get; set; }
    
    Guid? ExceptionDumpId { get; set; }
    
    string? ExceptionType { get; set; }
    
    LogLevel LogEventLevel { get; set; }

    string? Message { get; set; }

    string? TraceId { get; set; }
}