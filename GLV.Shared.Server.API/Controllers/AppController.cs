using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Security.Claims;
using GLV.Shared.Data;
using Microsoft.AspNetCore.Http;
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
    public UserManager<TUser> UserManager { get; set; }

    protected readonly ILogger<AppController<TUser>> Logger = logger ?? throw new ArgumentNullException(nameof(logger));

    [NonAction]
    public virtual IActionResult FromSuccessResult(SuccessResult result)
        => result.IsSuccess ? Ok() : FailureResult(result);

    [NonAction]
    public virtual async Task<IActionResult> FromSuccessResult(Task<SuccessResult> result)
        => FromSuccessResult(await result);

    [NonAction]
    public virtual IActionResult FromSuccessResult<T>(SuccessResult<T> result)
        => result.TryGetResult(out var val) ? Ok(val) : FailureResult(result);

    [NonAction]
    public virtual IActionResult FromSuccessResult<T>(SuccessResult<IAsyncEnumerable<T>> result)
        => result.TryGetResult(out var val) ? Ok(val) : FailureResult(result);

    [NonAction]
    public OkObjectResult Ok<T>(IAsyncEnumerable<T> value)
        => Ok(new AsyncResultData()
        {
            Data = value,
            AsyncEnumerableType = typeof(T)
        });

    [NonAction]
    public virtual async Task<IActionResult> FromSuccessResult<T>(Task<SuccessResult<T>> result)
        => FromSuccessResult(await result);

    [NonAction]
    public virtual IActionResult EntityNotFound(string entityName, string? query = null)
    {
        var errors = new ErrorList();
        errors.AddEntityNotFound(entityName, query);
        return FailureResult(errors);
    }

    [NonAction]
    public virtual IActionResult Forbidden()
    {
        var errors = new ErrorList();
        errors.AddNoPermission();
        return FailureResult(errors);
    }

    [NonAction]
    public virtual IActionResult Forbidden(object? value)
        => value is null ? Forbidden() : new ObjectResult(value) { StatusCode = (int)HttpStatusCode.Forbidden };

    [NonAction]
    public virtual IActionResult FailureResult(SuccessResult result)
        => new ObjectResult(result.ErrorMessages.Errors) { StatusCode = (int?)result.ErrorMessages.RecommendedCode ?? 418 };

    [NonAction]
    public virtual IActionResult FailureResult<T>(SuccessResult<T> result)
        => new ObjectResult(result.ErrorMessages.Errors) { StatusCode = (int?)result.ErrorMessages.RecommendedCode ?? 418 };

    [NonAction]
    public ValueTask<TUser?> GetUser()
        => HttpContext.GetUser(User, UserManager);

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
