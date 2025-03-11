using Azure.Identity;
using GLV.Shared.CosmosDB.Options;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection;

namespace GLV.Shared.CosmosDB;

public static class CosmosDBExtensions
{
    public const string DefaultDatabaseNameServiceKey = "GLV.Shared.CosmosDB:DefaultDatabaseName";

    public static IServiceCollection AddCosmosDB(
        this IServiceCollection services,
        CosmosDBOptions options
    )
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.Endpoint);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.DatabaseName);

        // Use a Singleton instance of the SocketsHttpHandler, which you can share across any HttpClient in your application
        SocketsHttpHandler socketsHttpHandler = new()
        {
            // Customize this value based on desired DNS refresh timer
            PooledConnectionLifetime = TimeSpan.FromMinutes(5)
        };

        if (string.IsNullOrWhiteSpace(options.AuthKeyOrResourceToken) is false)
        {
            services.AddSingleton(s => new CosmosClient(
                options.Endpoint, 
                options.AuthKeyOrResourceToken, 
                new CosmosClientOptions()
                {
                    HttpClientFactory = () => new HttpClient(socketsHttpHandler, disposeHandler: false)
                }
            ));
        }
        else
        {
            services.AddSingleton(s => new CosmosClient(
                options.Endpoint,
                new DefaultAzureCredential(),
                new CosmosClientOptions()
                {
                    HttpClientFactory = () => new HttpClient(socketsHttpHandler, disposeHandler: false)
                }
            ));
        }

        services.AddKeyedSingleton(DefaultDatabaseNameServiceKey, options.DatabaseName);
        return services;
    }
}
