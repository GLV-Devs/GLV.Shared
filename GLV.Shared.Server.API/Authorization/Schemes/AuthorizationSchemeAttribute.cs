using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;

namespace GLV.Shared.Server.API.Authorization.Schemes;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class AuthorizationSchemeAttribute(string scheme) : Attribute
{
    public string Scheme { get; } = scheme ?? throw new ArgumentNullException(nameof(scheme));
}

public static class AuthorizationSchemeHelpers
{
    public static void RegisterAuthorizationSchemes(this IServiceCollection serviceCollection, IConfiguration configuration)
    {
        var authBuilder = serviceCollection.AddAuthentication();
        List<(string, OpenApiSecurityScheme)> descriptions = [];

        foreach (var (type, attributes) in AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(x => x.GetTypes())
                .Select(x => (Type: x, Attributes: x.GetCustomAttributes<AuthorizationSchemeAttribute>()))
                .Where(x => x.Attributes is not null && x.Attributes.Any()))
        {
            if (type.IsAssignableTo(typeof(IAuthenticationSchemeDefinition)) is false)
                throw new InvalidOperationException("Cannot register a class decorated with AuthorizationSchemeAttribute that doesn't implement IAuthenticationSchemeDefinition");

            var def = (IAuthenticationSchemeDefinition)Activator.CreateInstance(type)!;
            def.DefineScheme(authBuilder, serviceCollection, configuration);
            descriptions.Add(def.DescribeScheme(serviceCollection, configuration));
        }

        serviceCollection.AddSwaggerGen(x =>
        {
            foreach (var (name, desc) in descriptions)
                x.AddSecurityDefinition(name, desc);
        });
    }
}