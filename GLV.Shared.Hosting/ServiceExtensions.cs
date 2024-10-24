using GLV.Shared.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace GLV.Shared.Hosting;

public static class ServiceExtensions
{
    public static IServiceScope GetServices<T>(this IServiceScope scope, out IEnumerable<T> services)
    {
        services = scope.ServiceProvider.GetServices<T>();
        return scope;
    }

    public static IServiceScope GetServices(this IServiceScope scope, Type serviceType, out IEnumerable<object?> services)
    {
        services = scope.ServiceProvider.GetServices(serviceType);
        return scope;
    }

    public static IServiceScope GetService<T>(this IServiceScope scope, out T? service)
    {
        service = scope.ServiceProvider.GetService<T>();
        return scope;
    }

    public static IServiceScope GetRequiredService<T>(this IServiceScope scope, out T service)
        where T : notnull
    {
        service = scope.ServiceProvider.GetRequiredService<T>();
        return scope;
    }

    public static IServiceScope GetService(this IServiceScope scope, Type serviceType, out object? service)
    {
        service = scope.ServiceProvider.GetService(serviceType);
        return scope;
    }

    public static IServiceScope GetRequiredService(this IServiceScope scope, Type serviceType, out object service)
    {
        service = scope.ServiceProvider.GetRequiredService(serviceType);
        return scope;
    }

    public static IServiceScope GetKeyedService<T>(this IServiceScope scope, object? serviceKey, out T? service)
    {
        service = scope.ServiceProvider.GetKeyedService<T>(serviceKey);
        return scope;
    }

    public static IServiceScope GetRequiredKeyedService<T>(this IServiceScope scope, object? serviceKey, out T service)
        where T : notnull
    {
        service = scope.ServiceProvider.GetRequiredKeyedService<T>(serviceKey);
        return scope;
    }
}
