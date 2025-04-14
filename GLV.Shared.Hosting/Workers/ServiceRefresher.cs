using GLV.Shared.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GLV.Shared.Hosting.Workers;

public class ServiceRefresher(ILogger<ServiceRefresher> log, IServiceProvider services) : BackgroundService
{
    private static bool servicesInitialized;
    private static readonly object initlock = new();

    public static async ValueTask InitializeRefreshableServices(IServiceProvider services, CancellationToken ct = default)
    {
        if (servicesInitialized) return;
        lock (initlock)
        {
            if (servicesInitialized) return;
            servicesInitialized = true;
        } // Thanks to this lock, even if the operation hasn't completed, it'll already be concurrently marked as such. So only the first requester will have to wait

        using (var scope = services.CreateScope().GetRequiredService<ILogger<ServiceRefresher>>(out var log).GetServices<IRefreshableService>(out var refreshable))
        {
            log.LogInformation("Initializing refreshable services");
            try
            {
                foreach (var init in refreshable)
                {
                    log.LogInformation("Initializing service {type}", init.GetType().Name);
                    await init.Initialize(scope.ServiceProvider);
                }
            }
            catch (Exception e)
            {
                log.LogCritical(e, "An unexpected exception was thrown whilst initializing Refreshable services");
                throw;
            }
            log.LogInformation("Finished initializing refreshable services");
        }

    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (servicesInitialized is false)
            throw new InvalidOperationException("Cannot start the ServiceRefresher background service without initializing services first, please call the static method: \"InitializeRefreshableServices\"");

        while (stoppingToken.IsCancellationRequested is false)
        {
            await Task.Delay(30_000, stoppingToken);

            try
            {
                using (var scope = services.CreateScope().GetServices<IRefreshableService>(out var refreshable))
                {
                    foreach (var refr in refreshable)
                        await refr.Refresh(scope.ServiceProvider);
                }
            }
            catch (Exception e)
            {
                log.LogCritical(e, "An unexpected exception was thrown whilst refreshing Refreshable services");
            }
        }
    }
}
