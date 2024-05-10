using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;

namespace GLV.Shared.Server.API.Authorization.Schemes;

public interface IAuthenticationSchemeDefinition
{
    public void DefineScheme(AuthenticationBuilder builder, IServiceCollection collection, IConfiguration configuration);

    public (string Name, OpenApiSecurityScheme Description) DescribeScheme(IServiceCollection collection, IConfiguration configuration);
}
