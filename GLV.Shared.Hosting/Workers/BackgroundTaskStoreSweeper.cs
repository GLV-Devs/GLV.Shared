using GLV.Shared.Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GLV.Shared.Hosting.Workers;

public static class BackgroundTaskStoreSweeperHelpers
{
    public static void UseBackgroundTasks(this IServiceCollection services)
    {
        var desc = new ServiceDescriptor(typeof(IHostedService), typeof(BackgroundTaskStoreSweeper), ServiceLifetime.Singleton);
        services.TryAddEnumerable(desc);
    }
}

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
