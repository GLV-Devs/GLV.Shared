using System.Diagnostics;
using System.Security.Claims;
using System.Text.Json;
using GLV.Shared.Common;
using GLV.Shared.Data;
using GLV.Shared.Server.API;
using GLV.Shared.Server.API.Authorization;
using GLV.Shared.Server.API.Controllers;
using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Swashbuckle.AspNetCore.Annotations;
using Microsoft.Extensions.DependencyInjection;
using GLV.Shared.Server.Identity.Services;
using GLV.Shared.Server.Data;

namespace GLV.Shared.Server.Identity.Controllers;

/// <summary>
/// Provides actions regarding an user account's information
/// </summary>
[NonController]
public abstract class GlvIdentityController<TUserInfo, TContext, TUserKey, TUserView, TUserCreateModel, TUserUpdateModel, TUserLoginModel>(
    IRepository<TUserInfo, TUserKey, TUserView, TUserCreateModel, TUserUpdateModel> repository,
    ILogger<GlvIdentityController<TUserInfo, TContext, TUserKey, TUserView, TUserCreateModel, TUserUpdateModel, TUserLoginModel>> logger,
    SignInManager<TUserInfo> signInManager,
    UserManager<TUserInfo> userManager,
    string? bearerTokenOptionsName = null,
    string? authorizationSchemeName = null
) : RepositoryController<TUserInfo, TContext, TUserInfo, TUserKey, TUserView, TUserCreateModel, TUserUpdateModel>(
        logger,
        repository
    )
    where TContext : DbContext
    where TUserInfo : class, IKeyed<TUserInfo, TUserKey>, IGlvIdentityUser
    where TUserKey : unmanaged, IEquatable<TUserKey>, IFormattable, IParsable<TUserKey>
    where TUserLoginModel : IGlvIdentityUserLoginModel
{
    /// <summary>
    /// CreateAccount - 
    /// Creates a new user account
    /// </summary>
    [HttpPost]
    public virtual Task<IActionResult> Create(TUserCreateModel creationModel)
        => CreateEntity(creationModel);

    /// <summary>
    /// Refresh - 
    /// Refreshes the current user session
    /// </summary>
    [HttpPatch("refresh")]
    public virtual async Task<IActionResult> Refresh(
        RefreshRequest refresh,
        [FromServices] IOptionsMonitor<BearerTokenOptions> bearerTokenOptions,
        [FromServices] TimeProvider timeProvider
    )
    {
        ErrorList errors = new();
        if (errors.IsEmptyString(refresh.RefreshToken))
            return FromSuccessResult(errors);

        var refreshTokenProtector = bearerTokenOptions.Get(bearerTokenOptionsName ?? Options.DefaultName).RefreshTokenProtector;
        var refreshTicket = refreshTokenProtector.Unprotect(refresh.RefreshToken);

        if (refreshTicket?.Properties?.ExpiresUtc is not { } expiresUtc || timeProvider.GetUtcNow() >= expiresUtc)
        {
            errors.AddBadRefreshToken();
            return FromSuccessResult(errors);
        }

        var refreshClaims = refreshTicket.Principal.Claims.Where(
            x => x.Type == UserSessionReader<TUserInfo, TUserKey>.UserSecurityRefreshTokenClaimsType
        ).ToList();

        var nameClaims = refreshTicket.Principal.Claims.Where(x => x.Type == ClaimTypes.NameIdentifier).ToList();

        if (refreshClaims.Count != 1 || Guid.TryParse(refreshClaims[0].Value, out var stamp) is false ||
            nameClaims.Count != 1 || TUserKey.TryParse(nameClaims[0].Value, null, out var userId) is false)
        {
            errors.AddBadRefreshToken();
            return FromSuccessResult(errors);
        }

        var user = await Context.Set<TUserInfo>().Where(x => x.Id.Equals(userId)).SingleOrDefaultAsync();

        if (user is null || user.RefreshTokenStamp != stamp)
        {
            errors.AddBadRefreshToken();
            return FromSuccessResult(errors);
        }

        user.RefreshTokenStamp = Guid.NewGuid();
        await Context.SaveChangesAsync();

        await HttpContext.SignInAsync(
            authorizationSchemeName, UserSessionReader<TUserInfo, TUserKey>.CreateUserClaimsPrincipal(user, authorizationSchemeName)
        );

        Logger.LogInformation("Succesfully refreshed user {user} ({userid})", user.UserName, user.Id);

        return Ok(HttpContext.Features.Get<GLVAccessTokenResponse>());
    }

    /// <summary>
	/// Login - 
    /// Logins as the user represented in <paramref name="userLogin"/>
	/// </summary>
    [HttpPatch]
    public virtual  async Task<IActionResult> Login(TUserLoginModel? userLogin)
    {
        ErrorList list = new();

        if (userLogin is null)
        {
            list.AddEmptyBody();
            return FailureResult(list);
        }

        if (string.IsNullOrWhiteSpace(userLogin.Identifier))
            list.AddBadUsername(userLogin.Identifier ?? "");

        if (string.IsNullOrWhiteSpace(userLogin.PasswordSHA256))
            list.AddBadPassword();

        if (list.Count > 0)
            return FailureResult(list);

        Debug.Assert(string.IsNullOrWhiteSpace(userLogin.Identifier) is false);
        Debug.Assert(string.IsNullOrWhiteSpace(userLogin.PasswordSHA256) is false);
        var user = await UserManager.FindByEmailAsync(userLogin.Identifier) ?? await UserManager.FindByNameAsync(userLogin.Identifier);
        if (user is null)
        {
            list.AddUserNotFound(userLogin.Identifier);
            return FailureResult(list);
        }

        Logger.LogInformation("Attempting to log in as user {user} ({userid})", userLogin.Identifier, user.Id);

        var result = await signInManager.CheckPasswordSignInAsync(user, userLogin.PasswordSHA256, false);

        if (result.Succeeded)
        {
            user.RefreshTokenStamp = Guid.NewGuid();
            await Context.SaveChangesAsync();
            var claimsPrincipal = UserSessionReader<TUserInfo, TUserKey>.CreateUserClaimsPrincipal(user, authorizationSchemeName);
            await HttpContext.SignInAsync(authorizationSchemeName, claimsPrincipal);

            Logger.LogInformation("Succesfully logged in as user {user} ({userid})", userLogin.Identifier, user.Id);

            return Ok(HttpContext.Features.Get<GLVAccessTokenResponse>());
        }
        else if (result.IsLockedOut)
        {
            Logger.LogInformation("Could not log in as user {user} ({userid}), because they're locked out", user.UserName!, user.Id);
            list.AddLoginLockedOut(user.UserName!);
        }
        else if (result.RequiresTwoFactor)
        {
            Logger.LogInformation("Could not log in as user {user} ({userid}), because they require 2FA", user.UserName!, user.Id);
            list.AddLoginRequires("2FA", user.UserName!);
        }
        else if (result.IsNotAllowed)
        {
            Logger.LogInformation("Could not log in as user {user} ({userid}), because they're not allowed to", user.UserName!, user.Id);
            return Forbidden();
        }
        else
        {
            Logger.LogInformation("Could not log in as user {user} ({userid})", user.UserName!, user.Id);
            list.AddBadLogin();
            return FailureResult(list);
        }

        list.AddActionDisallowed();
        return FailureResult(list);
    }

    /// <summary>
	/// Logout - 
    /// Logs out the current user
	/// </summary>
    [Authorize]
    [HttpDelete]
    public virtual  async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(authorizationSchemeName);
        return this.CreateServerResponseResult(null);
    }
}
