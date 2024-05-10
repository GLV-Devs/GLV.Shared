using System.Collections.Concurrent;
using GLV.Shared.EntityFramework;
using GLV.Shared.Server.API;
using GLV.Shared.Server.API.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace GLV.Shared.Server.API.Workers;

public class DatabaseLogSinkWriter<TLogEntry, TContext> : BackgroundService
    where TLogEntry : class, IDatabaseExecutionLogEntry
    where TContext : DbContext
{
    private readonly IOptions<DatabaseLogSinkOptions> Options;
    private readonly IServiceProvider Services;
    private readonly ConcurrentQueue<TLogEntry> Buffer;

    private DateTime LastUploaded = default;

    public DatabaseLogSinkWriter(IServiceProvider services, IOptions<DatabaseLogSinkOptions> options)
    {
        Services = services;
        Options = options;
        Buffer = services.GetRequiredKeyedService<ConcurrentQueue<TLogEntry>>("ExecutionLogQueue");
        AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (stoppingToken.IsCancellationRequested is false)
        {
            await Task.Delay(10_000, stoppingToken);
            if (Buffer.Count > Options.Value.MaximumBuffering || DateTime.Now > LastUploaded + Options.Value.UploadInterval)
            {
                LastUploaded = DateTime.Now;
                await Upload();
            }
        }
    }

    private async Task Upload()
    {
        using var scope = Services.CreateScope();

        using var context = scope.ServiceProvider.GetRequiredService<TContext>();
        while (Buffer.TryDequeue(out var le))
            context.Set<TLogEntry>().Add(le);

        await context.SaveChangesAsync();
    }

    private void CurrentDomain_ProcessExit(object? sender, EventArgs e)
    {
        Upload().ConfigureAwait(false).GetAwaiter().GetResult();
    }
}
