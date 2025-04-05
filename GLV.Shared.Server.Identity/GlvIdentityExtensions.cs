using GLV.Shared.Hosting;
using GLV.Shared.Server.API.Authorization.Implementations;
using GLV.Shared.Server.API.Controllers;
using GLV.Shared.Server.Identity.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
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
    public static async Task SeedAdminAccount<TUser>(
        this WebApplication app,
        Func<TUser> userSeedFactory,
        string? passwordSeed = null,
        string? userNameSeed = null,
        string? emailSeed = null
    ) where TUser : class, IGlvIdentityUser
    {
        emailSeed = string.IsNullOrWhiteSpace(emailSeed) ? "admin@admin.com" : emailSeed;
        userNameSeed = string.IsNullOrWhiteSpace(userNameSeed) ? "admin" : userNameSeed;

        using (app.Services.CreateScope().GetRequiredService<UserManager<TUser>>(out var manager))
        {
            var user = await manager.FindByEmailAsync(emailSeed);
            if (user is null)
            {
                var u = userSeedFactory();
                u.IsRoot = true;
                u.UserName = userNameSeed;
                u.Email = emailSeed;

                var result = await manager.CreateAsync(
                    u,
                    string.IsNullOrWhiteSpace(passwordSeed) ? "4dm1n$$p4ss" : passwordSeed
                );

                if (result.Succeeded is false)
                    throw new InvalidOperationException(
                        $"Could not seed the admin user due to the following errors:\n\t" +
                        string.Join("\n\t", result.Errors.Select(x => $"{x.Code}: {x.Description}"))
                    );
            }
        }
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
