using System.Diagnostics.CodeAnalysis;

namespace GLV.Shared.Server.API.Logging;
public class FileExceptionDumper(string basePath) : DefaultExceptionDumpFormatter, IExceptionDumper
{
    public string BasePath
    {
        get => basePath;
        set => basePath = value ?? throw new ArgumentNullException(nameof(value));
    }
    private string basePath = basePath ?? throw new ArgumentNullException(nameof(basePath));

    private IExceptionDumpFormatter? _f;

    [AllowNull]
    public IExceptionDumpFormatter Formatter
    {
        get => _f ?? this;
        set => _f = value;
    }

    public void WriteExceptionDump(Exception e, Guid id, IExceptionDumpFormatter? formatter)
    {
        Directory.CreateDirectory(BasePath);
        var fmt = formatter ?? Formatter;
        var file = Path.Combine(BasePath, $"{fmt.GetName(DateTimeOffset.Now, id, e)}.excp");
        using var writer = new StreamWriter(File.Open(file, FileMode.Create));
        fmt.WriteDump(e, writer);
    }
}
