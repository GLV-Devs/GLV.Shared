using GLV.Shared.Common;
using GLV.Shared.Data;
using GLV.Shared.Hosting;
using GLV.Shared.Server.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;

namespace GLV.Shared.EntityFramework;

public static class DbContextExtensions
{
    public static async Task InsertInCacheAndContext<TQueryKey, TItem>(this DbContext context, Cache<TQueryKey, TItem> cache, TQueryKey key, TItem item)
        where TQueryKey : notnull
        where TItem : class
    {
        context.Set<TItem>().Add(item);
        await context.SaveChangesAsync();
        cache.InsertItem(key, item);
    }

    public static ValueTask<TItem?> GetFromCacheOrContext<TKey, TItem>
        (this Cache<TKey, TItem> cache, DbContext context, TKey key, bool insertIfFoundOnContext = true)
        where TKey : unmanaged
        where TItem : class, IDbModel<TItem, TKey>
        => GetFromCacheOrContext(context, cache, key, insertIfFoundOnContext);

    public static ValueTask<TItem?> GetFromCacheOrContext<TItem, TQueryKey>
        (this Cache<TQueryKey, TItem> cache, DbContext context, TQueryKey key, Expression<Func<TItem, bool>> queryExpression, bool insertIfFoundOnContext = true)
        where TQueryKey : notnull
        where TItem : class
        => GetFromCacheOrContext(context, cache, key, queryExpression, insertIfFoundOnContext);

    public static async ValueTask<TItem?> GetFromCacheOrContext<TItem, TQueryKey>
        (this DbContext context, Cache<TQueryKey, TItem> cache, TQueryKey key, Expression<Func<TItem, bool>> queryExpression, bool insertIfFoundOnContext = true)
        where TQueryKey : notnull
        where TItem : class
    {
        var result = await cache.TryGetItem(key);
        if (result.TryGetResult(out var item))
            return item;

        item = await context.Set<TItem>().Where(queryExpression).FirstOrDefaultAsync();
        if (insertIfFoundOnContext)
            cache.InsertItem(key, item);

        return item;
    }

    public static async ValueTask<TItem?> GetFromCacheOrContext<TKey, TItem>
        (this DbContext context, Cache<TKey, TItem> cache, TKey key, bool insertIfFoundOnContext = true)
        where TKey : unmanaged
        where TItem : class, IDbModel<TItem, TKey>
    {
        var result = await cache.TryGetItem(key);
        if (result.TryGetResult(out var item))
            return item;

        item = await context.Set<TItem>().FindAsync(key);
        if (insertIfFoundOnContext)
            cache.InsertItem(key, item);

        return item;
    }

    public static async Task MigrateDatabase<TContext>(this IServiceProvider services) where TContext : DbContext
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TContext>();
        await db.Database.MigrateAsync();
    }

    public static async Task InitDatabase<TContext>(this IServiceProvider services, Func<TContext, Task>? debugSeed = null) where TContext : DbContext
    {
        bool resetdb = Environment.CommandLine.Contains(" -reset-db");
        using (services.CreateScope().GetRequiredService<TContext>(out var context))
        {
            if (resetdb)
            {
                Console.WriteLine(" >!> Resetting Database");
                await context.Database.EnsureDeletedAsync();
            }

            await context.Database.MigrateAsync();

            Console.WriteLine(" >!> DataBase Initialized");

            if (resetdb)
            {
                if (debugSeed is not null && Environment.CommandLine.Contains(" -s"))
                {
                    Console.WriteLine(" >!> Seeding reset Database with debugging info");
                    await debugSeed.Invoke(context);
                    await context.SaveChangesAsync();
                }

                if (Environment.CommandLine.Contains(" -k"))
                {
                    Console.WriteLine(" >!> Found the -k flag in -reset-db, killing app");
                    Environment.Exit(0);
                }
            }
        }
    }

    /// <summary>
    /// Attempts to handle a database update exception, returning instead an ErrorList if possible
    /// </summary>
    /// <returns><see langword="true"/> if the error was managable and <paramref name="errors"/> contains a value, <see langword="false"/> otherwise and if the exception should propagate</returns>
    public static bool TryHandleDatabaseException(this DbUpdateException exception, [NotNullWhen(true)] out ErrorList errors)
    {
        errors = new();
        var msg = exception.InnerException!.Message;

        if (msg.Contains("foreign key", StringComparison.OrdinalIgnoreCase))
        {
            var match = DataRegexes.DatabaseExceptionMessageForeignKey().Match(msg);
            if (match.Success)
            {
                if (match.Groups.TryGetValue("entity", out var group))
                {
                    errors.AddEntityNotFound(group.Value, null);
                    return true;
                }
            }
        }
        else if (msg.Contains("Duplicate", StringComparison.OrdinalIgnoreCase))
        {
            var match = DataRegexes.DatabaseExceptionMessageDuplicateKey().Match(msg);
            if (match.Success)
            {
                if (match.Groups.TryGetValue("index", out var group))
                {
                    if (group.Value.Equals("primary", StringComparison.OrdinalIgnoreCase))
                        errors.AddUniqueEntityAlreadyExists(null);
                    else
                        errors.AddUniqueValueForPropertyAlreadyExists(group.Value, match.Groups.TryGetValue("key", out var val) ? val.Value : null);
                    return true;
                }
            }
        }

        return false;
    }

    public static async ValueTask<SuccessResult> TrySaveChanges(this DbContext context)
    {
        try
        {
            await context.SaveChangesAsync();
            return SuccessResult.Success;
        }
        catch (DbUpdateException e)
        {
            if (e.TryHandleDatabaseException(out var errors))
                return errors;
            throw;
        }
    }
}
