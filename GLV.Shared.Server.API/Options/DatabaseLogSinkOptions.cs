using GLV.Shared.Server.API;

namespace GLV.Shared.Server.API.Options;

[RegisterOptions(OptionsName = "DatabaseLogSink")]
public class DatabaseLogSinkOptions
{
    public int MaximumBuffering { get; set; }
    public TimeSpan UploadInterval { get; set; }
}
