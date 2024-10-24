using Microsoft.Extensions.DependencyInjection;

namespace GLV.Shared.Hosting;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public sealed class RegisterServiceAttribute(Type? type = null) : Attribute
{
    public Type? Interface { get; init; } = type;
    public ServiceLifetime Lifetime { get; init; } = ServiceLifetime.Scoped;
    public string? StringServiceKey { get; init; }
}