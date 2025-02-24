using GLV.Shared.CosmosDB.Options;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection;

namespace GLV.Shared.CosmosDB;

public static class CosmosDBExtensions
{
    internal const string DefaultDatabaseNameServiceKey = "GLV.Shared.CosmosDB:DefaultDatabaseName";

    public static IServiceCollection AddCosmosDB(
        this IServiceCollection services,
        CosmosDBOptions options
    )
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.Endpoint);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.AuthKeyOrResourceToken);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.DatabaseName);

        services.AddScoped(s => new CosmosClient(options.Endpoint, options.AuthKeyOrResourceToken, options.ClientOptions));
        services.AddKeyedSingleton(DefaultDatabaseNameServiceKey, options.DatabaseName);
        return services;
    }
}
