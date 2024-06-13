using GLV.Shared.Server.API.Configuration;
using GLV.Shared.Server.API.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace GLV.Shared.Server.API;

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

    public static IServiceCollection ConfigureGLVDatabase<TContext>(
        this IServiceCollection services, 
        IHostApplicationBuilder builder,
        string sqlServerMigrationsAssemblyName,
        string mySqlMigrationsAssemblyName,
        string sqliteMigrationsAssemblyName,
        string? configSectionName = null,
        bool setAsDefaultDbContext = false
    )
        where TContext : DbContext
    {
        var contextName = typeof(TContext).Name;

        var dbconf = builder.Configuration.GetRequiredSection("DatabaseConfig").GetRequiredSection(configSectionName ?? contextName).Get<ServerDatabaseConfiguration?>()
            ?? throw new InvalidDataException("TContext parameter under DatabaseConfig section returned null");

        if (dbconf.DatabaseType is DatabaseType.SQLServer)
        {
            if (string.IsNullOrWhiteSpace(dbconf.SQLServerConnectionString))
                throw new InvalidOperationException($"SQLServerConnectionString for {contextName} is not set despite SQLServer being selected");

            services.AddDbContext<TContext>(x => x.UseSqlServer(
                dbconf.SQLServerConnectionString,
                o => o.MigrationsAssembly(sqlServerMigrationsAssemblyName)
            ));
        }
        else if (dbconf.DatabaseType is DatabaseType.MySQL)
        {
            if (string.IsNullOrWhiteSpace(dbconf.SQLServerConnectionString))
                throw new InvalidOperationException($"SQLServerConnectionString for {contextName} is not set despite MySQL being selected");

            services.AddDbContext<TContext>(x => x.UseMySql(
                dbconf.SQLServerConnectionString,
                ServerVersion.AutoDetect(dbconf.SQLServerConnectionString),
                o => o.MigrationsAssembly(mySqlMigrationsAssemblyName)
            ));
        }
        else if (dbconf.DatabaseType is DatabaseType.SQLite)
        {
            if (string.IsNullOrWhiteSpace(dbconf.SQLiteConnectionString))
                throw new InvalidOperationException($"SQLiteConnectionString for {contextName} is not set despite SQLite being selected");

            var conns = DatabaseConfiguration.FormatConnectionString(dbconf.SQLiteConnectionString);
            var path = GLV.Shared.Data.DataRegexes.SQLiteConnectionStringFilePath().Match(conns).Groups[1].ValueSpan;
            var dir = Path.GetDirectoryName(path);
            Directory.CreateDirectory(new string(dir));
            services.AddDbContext<TContext>(x => x.UseSqlite(
                conns,
                o => o.MigrationsAssembly(sqliteMigrationsAssemblyName)
            ));
        }
        else
            throw new InvalidDataException($"Unknown Database Type: {dbconf.DatabaseType}");

        if (setAsDefaultDbContext)
            services.AddScoped<DbContext, TContext>();

        return services;
    }
}
