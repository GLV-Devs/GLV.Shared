using System.Reflection;
using System.Xml.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace GLV.Shared.Server.API;
public static class ServicesHelper
{
    public static void RegisterDecoratedWorkers(this IServiceCollection services)
    {
        foreach (var (type, attributes) in AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(x => x.GetTypes())
                .Select(x => (Type: x, Attributes: x.GetCustomAttributes<RegisterWorkerAttribute>()))
                .Where(x => x.Attributes is not null && x.Attributes.Any()))
        {
            var desc = new ServiceDescriptor(typeof(IHostedService), type, ServiceLifetime.Singleton);
            services.TryAddEnumerable(desc);
        }
    }

    public static void RegisterDecoratedServices(this IServiceCollection serviceCollection)
    {
        foreach (var (type, attributes) in AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(x => x.GetTypes())
                .Select(x => (Type: x, Attributes: x.GetCustomAttributes<RegisterServiceAttribute>()))
                .Where(x => x.Attributes is not null && x.Attributes.Any()))
        {
            foreach (var attr in attributes)
            {
                if (string.IsNullOrWhiteSpace(attr.StringServiceKey) is false)
                    serviceCollection.Add(new ServiceDescriptor(attr.Interface ?? type, attr.StringServiceKey, type, attr.Lifetime));
                else
                    serviceCollection.Add(new ServiceDescriptor(attr.Interface ?? type, type, attr.Lifetime));
            }
        }
    }

    public static void AddBoundOptions<TOptions>(
        this IServiceCollection services,
        IConfiguration config,
        string? section = null,
        string? optionsName = null
    ) => services.AddBoundOptions(typeof(TOptions), config, section, optionsName);

    public static void AddBoundOptions(
        this IServiceCollection services,
        Type type,
        IConfiguration config,
        string? section = null,
        string? optionsName = null
    )
    {
        services.AddOptions();

        var optionsType = typeof(IOptions<>).MakeGenericType(type);
        var configureOptionsType = typeof(IConfigureOptions<>).MakeGenericType(type);
        var configureNamedOptionsType = typeof(ConfigureNamedOptions<>).MakeGenericType(type);
        var optionsConfigurationCallbackType = typeof(Action<>).MakeGenericType(type);
        var configureNamedOptionsConstructor = configureNamedOptionsType.GetConstructor(
            new Type[] { typeof(string), optionsConfigurationCallbackType }
        )!;

        var decoratedOptionsClosureType = typeof(DecoratedOptionsClosure<>).MakeGenericType(type);
        var decoratedOptionsClosureConstructor = decoratedOptionsClosureType.GetConstructor(
            new Type[] { typeof(string), typeof(IConfiguration) }
        )!;

        RegisterOptions(
            services,
            optionsName,
            section,
            type,
            config,
            optionsType,
            decoratedOptionsClosureConstructor,
            configureOptionsType,
            configureNamedOptionsConstructor,
            optionsConfigurationCallbackType
        );
    }

    public static void RegisterDecoratedOptions(this IServiceCollection services, IConfiguration config)
    {
        services.AddOptions();
        foreach (var (type, attributes) in AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(x => x.GetTypes())
                .Select(x => (Type: x, Attributes: x.GetCustomAttributes<RegisterOptionsAttribute>()))
                .Where(x => x.Attributes is not null && x.Attributes.Any()))
        {
            var optionsType = typeof(IOptions<>).MakeGenericType(type);
            var configureOptionsType = typeof(IConfigureOptions<>).MakeGenericType(type);
            var configureNamedOptionsType = typeof(ConfigureNamedOptions<>).MakeGenericType(type);
            var optionsConfigurationCallbackType = typeof(Action<>).MakeGenericType(type);
            var configureNamedOptionsConstructor = configureNamedOptionsType.GetConstructor(
                new Type[] { typeof(string), optionsConfigurationCallbackType }
            )!;

            var decoratedOptionsClosureType = typeof(DecoratedOptionsClosure<>).MakeGenericType(type);
            var decoratedOptionsClosureConstructor = decoratedOptionsClosureType.GetConstructor(
                new Type[] { typeof(string), typeof(IConfiguration) }
            )!;

            foreach (var attr in attributes)
                RegisterOptions(
                    services,
                    attr.OptionsName,
                    attr.SectionName,
                    type,
                    config,
                    optionsType,
                    decoratedOptionsClosureConstructor,
                    configureOptionsType,
                    configureNamedOptionsConstructor,
                    optionsConfigurationCallbackType
                );
        }
    }

    private static void RegisterOptions(
            IServiceCollection services,
            string? name,
            string? section,
            Type type,
            IConfiguration config,
            Type optionsType,
            ConstructorInfo decoratedOptionsClosureConstructor,
            Type configureOptionsType,
            ConstructorInfo configureNamedOptionsConstructor,
            Type optionsConfigurationCallbackType
            )
    {
        name ??= Options.DefaultName;
        section ??= type.Name;

        var closure = decoratedOptionsClosureConstructor.Invoke(new object[] { section, config });
        var instance = configureNamedOptionsConstructor.Invoke(new object[]
        {
            name,
            Delegate.CreateDelegate(optionsConfigurationCallbackType, closure, "Bind")
        });

        services.AddSingleton(configureOptionsType, instance);
    }

    private class DecoratedOptionsClosure<T>
    {
        readonly string bindingName;
        readonly IConfiguration config;

        public DecoratedOptionsClosure(string bindingName, IConfiguration config)
        {
            this.bindingName = bindingName ?? throw new ArgumentNullException(nameof(bindingName));
            this.config = config ?? throw new ArgumentNullException(nameof(config));
        }

        public void Bind(T options) => config.GetSection(bindingName).Bind(options);
    }
}
