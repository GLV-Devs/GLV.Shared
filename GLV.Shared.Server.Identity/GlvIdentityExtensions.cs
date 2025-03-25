using GLV.Shared.Server.API.Authorization.Implementations;
using GLV.Shared.Server.API.Controllers;
using GLV.Shared.Server.Identity.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GLV.Shared.Server.Identity;

public static class GlvIdentityExtensions
{
    public static void AddGlvIdentityControllerServices<TUserInfo, TUserKey>(this IServiceCollection services, string? authorizationSchemeName = null)
        where TUserInfo : IGlvIdentityUser
        where TUserKey : unmanaged, IEquatable<TUserKey>, IFormattable, IParsable<TUserKey>
    {
        services.AddScoped<UserSessionReader<TUserInfo, TUserKey>>(s => new(s.GetRequiredService<SessionManager>(), authorizationSchemeName));
    }

    public static UserSessionInfo<TUserInfo, TUserKey>? GetUserSession<TUserInfo, TUserKey>(this ControllerBase controller)
        where TUserInfo : IGlvIdentityUser
        where TUserKey : unmanaged, IEquatable<TUserKey>, IFormattable, IParsable<TUserKey>
    {
        return controller.HttpContext.RequestServices
            .GetRequiredService<UserSessionReader<TUserInfo, TUserKey>>()
            .GetSessionInfo(controller.HttpContext.User);
    }
}
