using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Security.Claims;
using GLV.Shared.Data;
using Microsoft.AspNetCore.Http;
using GLV.Shared.Server.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
namespace GLV.Shared.Server.API.Controllers;

public static class ControllerHelpers
{
    public static async ValueTask<TUser?> GetUser<TUser>(this HttpContext context, ClaimsPrincipal user, UserManager<TUser> userManager)
        where TUser : class
    {
        var u = context.Features.Get<TUser>();
        if (u is not null)
            return u;

        u = await userManager.GetUserAsync(user);
        context.Features.Set(u);
        return u;
    }
}

public abstract class AppController<TUser>(ILogger<AppController<TUser>> logger) : Controller
    where TUser : class
{
    public AppController(ILogger<AppController<TUser>> logger, UserManager<TUser> userManager) : this(logger)
    {
        UserManager = userManager;
    }

    [FromServices]
    public UserManager<TUser> UserManager { get; set; } = null!;

    protected readonly ILogger<AppController<TUser>> Logger = logger ?? throw new ArgumentNullException(nameof(logger));

    [NonAction]
    public ValueTask<TUser?> GetUser()
        => HttpContext.GetUser(User, UserManager);

    [NonAction]
    protected bool ProcessCommandToken(string? token, ref ErrorList errors, [NotNullWhen(true)] out string? command)
    {
        command = null;

        if (token is null || CryptoHelpers.TryDecodeCommandToken(token, out command, out var exp, out var valid) is false)
            errors.AddInvalidCommandToken();

        else if (DateTimeOffset.Now > exp)
            errors.AddCommandTokenExpired();

        else if (DateTimeOffset.Now > valid is false)
            errors.AddCommandTokenNotYetAvailable();

        return errors.Count > 0;
    }
}
