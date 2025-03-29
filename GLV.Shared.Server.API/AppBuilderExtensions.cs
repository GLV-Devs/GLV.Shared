using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GLV.Shared.Server.API;

public static class AppBuilderExtensions
{
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
