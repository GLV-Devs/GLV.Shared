using GLV.Shared.Common;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GLV.Shared.Hosting.Workers;

public class BackgroundTaskStoreSweeper(ILogger<BackgroundTaskStoreSweeper> logger) : BackgroundService
{
    private readonly ILogger<BackgroundTaskStoreSweeper> logger = logger ?? throw new ArgumentNullException(nameof(logger));

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (stoppingToken.IsCancellationRequested is false)
        {
            await BackgroundTaskStore.Sweep(stoppingToken);
            await Task.Delay(1000, stoppingToken);
        }
    }
}
