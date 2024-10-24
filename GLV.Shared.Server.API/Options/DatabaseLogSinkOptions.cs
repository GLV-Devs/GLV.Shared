using GLV.Shared.Server.API;
using GLV.Shared.Hosting;
using GLV.Shared.Hosting.Workers;

namespace GLV.Shared.Server.API.Options;

[RegisterOptions(OptionsName = "DatabaseLogSink")]
public class DatabaseLogSinkOptions
{
    public int MaximumBuffering { get; set; }
    public TimeSpan UploadInterval { get; set; }
}
