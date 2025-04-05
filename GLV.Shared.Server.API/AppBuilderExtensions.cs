using GLV.Shared.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GLV.Shared.Server.API;

public static class AppBuilderExtensions
{
    public static async Task InitDatabase<TContext>(this WebApplication app, Func<TContext, Task>? debugSeed = null) where TContext : DbContext
    {
        bool resetdb = Environment.CommandLine.Contains(" -reset-db");
        using (app.Services.CreateScope().GetRequiredService<TContext>(out var context))
        {
            if (resetdb)
            {
                Console.WriteLine(" >!> Resetting Database");
                await context.Database.EnsureDeletedAsync();
            }

            await context.Database.MigrateAsync();

            Console.WriteLine(" >!> DataBase Initialized");

            if (Environment.CommandLine.Contains(" -reset-db -k"))
            {
                Console.WriteLine(" >!> Found the -k flag in -reset-db, killing app");
                Environment.Exit(0);
            }

            else if (debugSeed is not null && Environment.CommandLine.Contains(" -reset-db -s"))
            {
                Console.WriteLine(" >!> Seeding reset Database with debugging info");
                await debugSeed.Invoke(context);
                await context.SaveChangesAsync();
            }
        }
    }

    public static WebApplicationBuilder ConfigureCors(this WebApplicationBuilder builder)
    {
        var corsconf = builder.Configuration.GetRequiredSection("CorsOrigins").Get<string[]>()
        ?? throw new InvalidDataException("CorsOrigins returned null");

        if (corsconf.Length is 0)
            throw new InvalidDataException("No CORS Origins configured");

        builder.Services.AddCors(options => options.AddDefaultPolicy(builder
            => builder
                .WithOrigins(corsconf) // Esto es una lista que saco de la config
                .AllowAnyMethod()
                .AllowCredentials()
                .AllowAnyHeader()
                .WithExposedHeaders("Access-Control-Allow-Origin")
        ));

        return builder;
    }
}
