using GLV.Shared.EntityFramework.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;

namespace GLV.Shared.EntityFramework;

public static class DatabaseServiceExtensions
{
    public static IServiceCollection ConfigureGLVDatabase<TContext>(
        this IServiceCollection services,
        IHostApplicationBuilder builder,
        string? sqlServerMigrationsAssemblyName = null,
        string? mySqlMigrationsAssemblyName = null,
        string? sqliteMigrationsAssemblyName = null,
        string? configSectionName = null,
        Action<object>? optionsBuilder = null,
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
                o =>
                {
                    if (string.IsNullOrWhiteSpace(sqlServerMigrationsAssemblyName) is false)
                        o.MigrationsAssembly(sqlServerMigrationsAssemblyName);
                    o.EnableRetryOnFailure();
                    optionsBuilder?.Invoke(o);
                }
            ));
        }
        else if (dbconf.DatabaseType is DatabaseType.MySQL)
        {
            if (string.IsNullOrWhiteSpace(dbconf.SQLServerConnectionString))
                throw new InvalidOperationException($"SQLServerConnectionString for {contextName} is not set despite MySQL being selected");

            services.AddDbContext<TContext>(x => x.UseMySql(
                dbconf.SQLServerConnectionString,
                ServerVersion.AutoDetect(dbconf.SQLServerConnectionString),
                o =>
                {
                    if (string.IsNullOrWhiteSpace(mySqlMigrationsAssemblyName) is false)
                        o.MigrationsAssembly(mySqlMigrationsAssemblyName);
                    o.EnableRetryOnFailure(10);
                    o.CommandTimeout(120);
                    o.SchemaBehavior(MySqlSchemaBehavior.Translate, (schema, obj) => $"{schema}.{obj}");
                    optionsBuilder?.Invoke(o);
                }
            ));
        }
        else if (dbconf.DatabaseType is DatabaseType.SQLite)
        {
            if (string.IsNullOrWhiteSpace(dbconf.SQLiteConnectionString))
                throw new InvalidOperationException($"SQLiteConnectionString for {contextName} is not set despite SQLite being selected");

            var conns = DatabaseConfiguration.FormatConnectionString(dbconf.SQLiteConnectionString);
            var path = Data.DataRegexes.SQLiteConnectionStringFilePath().Match(conns).Groups[1].ValueSpan;
            var dir = new string(Path.GetDirectoryName(path));
            Directory.CreateDirectory(dir);

            Console.WriteLine($" >!> Using SQLite at {dir} for Context: {typeof(TContext).Name}");
            services.AddDbContext<TContext>(x => x.UseSqlite(
                conns,
                o =>
                {
                    if (string.IsNullOrWhiteSpace(sqliteMigrationsAssemblyName) is false)
                        o.MigrationsAssembly(sqliteMigrationsAssemblyName);
                    optionsBuilder?.Invoke(o);
                }));
        }
        else
            throw new InvalidDataException($"Unknown Database Type: {dbconf.DatabaseType}");

        if (setAsDefaultDbContext)
            services.AddScoped<DbContext, TContext>();

        return services;
    }
}
