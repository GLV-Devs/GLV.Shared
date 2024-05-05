namespace GLV.Shared.Server.API.Logging;

public interface IExceptionDumpFormatter
{
    public string GetName(DateTimeOffset dateTimeThrown, Guid id, Exception e);

    public string GetDumpContent(Exception e);

    public void WriteDump(Exception e, TextWriter writer);
}
