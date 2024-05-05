namespace GLV.Shared.Server.API.Logging;

public interface IExceptionDumper
{
    public IExceptionDumpFormatter? Formatter { get; set; }
    public void WriteExceptionDump(Exception e, Guid id, IExceptionDumpFormatter? formatter);
}
