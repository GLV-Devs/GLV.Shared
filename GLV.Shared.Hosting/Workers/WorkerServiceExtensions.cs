using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace GLV.Shared.Hosting.Workers;

public static class WorkerServiceExtensions
{
    public static void UseServiceRefresher(this IServiceCollection services)
    {
        var desc = new ServiceDescriptor(typeof(IHostedService), typeof(ServiceRefresher), ServiceLifetime.Singleton);
        services.TryAddEnumerable(desc);
    }

    public static void UseBackgroundTasks(this IServiceCollection services)
    {
        var desc = new ServiceDescriptor(typeof(IHostedService), typeof(BackgroundTaskStoreSweeper), ServiceLifetime.Singleton);
        services.TryAddEnumerable(desc);
    }
}
