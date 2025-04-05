using System.Diagnostics;
using System.Security.Claims;
using System.Text.Json;
using GLV.Shared.Common;
using GLV.Shared.Data;
using GLV.Shared.Server.API;
using GLV.Shared.Server.API.Controllers;
using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Swashbuckle.AspNetCore.Annotations;
using GLV.Shared.Server.Identity.Services;
using GLV.Shared.Server.Data;
using System.Net;
using GLV.Shared.Server.Client.Models;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.AspNetCore.Http;
using System.Diagnostics.CodeAnalysis;
using GLV.Shared.DataTransfer;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace GLV.Shared.Server.Identity.Controllers;

/// <summary>
/// Provides actions regarding an user account's information
/// </summary>
[NonController]
public abstract class GlvIdentityController<
    TUserInfo, 
    TContext, 
    TUserKey, 
    TUserView, 
    TUserCreateModel, 
    TUserUpdateModel, 
    TUserLoginModel,
    TUserSessionView
>(
    IRepository<TUserInfo, TUserKey, TUserView, TUserCreateModel, TUserUpdateModel> repository,
    ILogger<GlvIdentityController<TUserInfo, TContext, TUserKey, TUserView, TUserCreateModel, TUserUpdateModel, TUserLoginModel, TUserSessionView>> logger,
    SignInManager<TUserInfo> signInManager,
    UserManager<TUserInfo> userManager
) : AppController<TUserInfo>(logger)
    where TContext : DbContext
    where TUserInfo : class, IKeyed<TUserInfo, TUserKey>, IGlvIdentityUser
    where TUserKey : unmanaged, IEquatable<TUserKey>, IFormattable, IParsable<TUserKey>
    where TUserLoginModel : IGlvIdentityUserLoginModel
    where TUserSessionView : IGlvIdentitySessionView
{
    [field: AllowNull]
    public string AuthorizationSchemeName { get => field ?? BearerTokenDefaults.AuthenticationScheme; internal set; }

    public string? BearerTokenOptionsName { get; internal set; }

    public TContext Context { get; internal set; } = null!;

    protected internal virtual async Task<IdentityResponse> Create(TUserCreateModel creationModel)
        => CreateResult(await repository.CreateEntity(creationModel));

    protected internal virtual async Task<IdentityResponse> ChangePassword(ChangeUserPasswordModel model)
    {
        var session = this.GetUserSession<TUserInfo, TUserKey>();
        Debug.Assert(session != null);
        var key = session.RequesterUserId;
        var user = await Context.Set<TUserInfo>().Where(x => x.Id.Equals(key)).SingleOrDefaultAsync();
        if (user is null)
        {
            await HttpContext.SignOutAsync(AuthorizationSchemeName);
            return this.CreateErrorResult(HttpStatusCode.NotFound);
        }

        if (string.IsNullOrWhiteSpace(model.OldPassword))
            return this.CreateErrorResult(HttpStatusCode.BadRequest, ErrorMessages.EmptyProperty(nameof(model.OldPassword)));

        if (string.IsNullOrWhiteSpace(model.NewPassword))
            return this.CreateErrorResult(HttpStatusCode.BadRequest, ErrorMessages.EmptyProperty(nameof(model.NewPassword)));

        var result = await userManager.ChangePasswordAsync(user, model.OldPassword, model.NewPassword);
        if (result.Succeeded)
        {
            await HttpContext.SignOutAsync(AuthorizationSchemeName);

            user.RefreshTokenStamp = Guid.NewGuid();
            await Context.SaveChangesAsync();

            return new(HttpStatusCode.NoContent, null);
        }

        ErrorList errors = new();
        errors.AddIdentityErrors(result);
        return this.CreateResult(errors);
    }

    protected virtual ValueTask<bool> CanChangePermissions(ulong permissions) => ValueTask.FromResult(true);

    /// <summary>
    /// ChangePermissions - 
    /// Changes the specified user's password
    /// </summary>
    /// <returns></returns>
    protected internal virtual async Task<IdentityResponse> ChangePermissions(TUserKey userId, ChangeUserPermissionsModel model)
    {
        var session = this.GetUserSession<TUserInfo, TUserKey>();
        Debug.Assert(session != null);

        var user = await Context.Set<TUserInfo>().Where(x => x.Id.Equals(userId)).SingleOrDefaultAsync();

        if (user is null)
            return this.CreateErrorResult(HttpStatusCode.NotFound, ErrorMessages.EntityNotFound(nameof(TUserInfo), $"id:{userId}"));

        if (user.IsRoot || user.BaseLevel >= session.UserLevel)
            return this.CreateErrorResult(HttpStatusCode.Forbidden, IdentityErrorMessages.ModifiedUserLevelTooHigh());

        if (model.NewPermissions is ulong newPermissions)
        {
            if (session.IsRoot is false && (
                await CanChangePermissions(session.GlobalPermissions) is false
             || (newPermissions & (~session.GlobalPermissions)) > 0
            ))
                return this.CreateErrorResult(HttpStatusCode.Forbidden, IdentityErrorMessages.InvalidUserPermissions());

            user.Permissions = newPermissions;
        }

        if (model.NewBaseLevel is uint newBaseLevel)
        {
            if (session.IsRoot is false && session.UserLevel <= newBaseLevel)
                return this.CreateErrorResult(HttpStatusCode.Forbidden, IdentityErrorMessages.CurrentUserLevelTooLow());

            user.BaseLevel = newBaseLevel;
        }

        await Context.SaveChangesAsync();

        return new(HttpStatusCode.NoContent, null);
    }
    /// <summary>
    /// Refresh - 
    /// Refreshes the current user session
    /// </summary>
    protected internal virtual async Task<IdentityResponse> Refresh(
        RefreshRequest refresh,
        [FromServices] IOptionsMonitor<BearerTokenOptions> bearerTokenOptions,
        [FromServices] TimeProvider timeProvider
    )
    {
        ErrorList errors = new();
        if (errors.IsEmptyString(refresh.RefreshToken))
            return CreateErrorResult(errors);

        var refreshTokenProtector = bearerTokenOptions.Get(BearerTokenOptionsName ?? Options.DefaultName).RefreshTokenProtector;
        var refreshTicket = refreshTokenProtector.Unprotect(refresh.RefreshToken);

        if (refreshTicket?.Properties?.ExpiresUtc is not { } expiresUtc || timeProvider.GetUtcNow() >= expiresUtc)
        {
            errors.AddBadRefreshToken();
            return CreateErrorResult(errors);
        }

        var refreshClaims = refreshTicket.Principal.Claims.Where(
            x => x.Type == UserSessionReader<TUserInfo, TUserKey>.UserSecurityRefreshTokenClaimsType
        ).ToList();

        var nameClaims = refreshTicket.Principal.Claims.Where(x => x.Type == ClaimTypes.NameIdentifier).ToList();

        if (refreshClaims.Count != 1 || Guid.TryParse(refreshClaims[0].Value, out var stamp) is false ||
            nameClaims.Count != 1 || TUserKey.TryParse(nameClaims[0].Value, null, out var userId) is false)
        {
            errors.AddBadRefreshToken();
            return CreateErrorResult(errors);
        }

        var user = await Context.Set<TUserInfo>().Where(x => x.Id.Equals(userId)).SingleOrDefaultAsync();

        if (user is null || user.RefreshTokenStamp != stamp)
        {
            errors.AddBadRefreshToken();
            return CreateErrorResult(errors);
        }

        user.RefreshTokenStamp = Guid.NewGuid();
        await Context.SaveChangesAsync();

        await HttpContext.SignInAsync(
            AuthorizationSchemeName, UserSessionReader<TUserInfo, TUserKey>.CreateUserClaimsPrincipal(user, AuthorizationSchemeName)
        );

        Logger.LogInformation("Succesfully refreshed user {user} ({userid})", user.UserName, user.Id);

        return CreateResult(HttpContext.Features.Get<GLVAccessTokenResponse>());
    }

    protected internal virtual IdentityResponse GetSessionInfo()
    {
        var session = this.GetUserSession<TUserInfo, TUserKey>();
        Debug.Assert(session != null);
        return this.CreateResult(GetUserSessionView(session));
    }

    protected abstract TUserSessionView GetUserSessionView(UserSessionInfo<TUserInfo, TUserKey> sessionInfo);

    protected IdentityResponse CreateErrorResult(HttpStatusCode code, params Span<ErrorMessage> messages)
    {
        var err = new ErrorList(code);
        foreach (var e in messages)
            err.AddError(e);

        var resp = ServerResponse.CreateServerResponse(err, HttpContext.TraceIdentifier);

        return new(code, resp);
    }

    protected IdentityResponse CreateErrorResult(ErrorList errors)
        => new(errors.RecommendedCode ?? HttpStatusCode.BadRequest, ServerResponse.CreateServerResponse(errors, HttpContext.TraceIdentifier));

    protected IdentityResponse CreateResult<TData>(TData data)
        => new(HttpStatusCode.OK, data.CreateServerResponse(HttpContext.TraceIdentifier));

    /// <summary>
	/// Login - 
    /// Logins as the user represented in <paramref name="userLogin"/>
	/// </summary>
    protected internal virtual async Task<IdentityResponse> Login(TUserLoginModel? userLogin)
    {
        if (userLogin is null)
            return CreateErrorResult(HttpStatusCode.BadRequest, ErrorMessages.EmptyBody());

        ErrorList list = new();

        if (string.IsNullOrWhiteSpace(userLogin.Identifier))
            list.AddBadUsername(userLogin.Identifier ?? "");

        if (string.IsNullOrWhiteSpace(userLogin.Password))
            list.AddBadPassword();

        if (list.Count > 0)
            return CreateErrorResult(list);

        Debug.Assert(string.IsNullOrWhiteSpace(userLogin.Identifier) is false);
        Debug.Assert(string.IsNullOrWhiteSpace(userLogin.Password) is false);
        var user = await UserManager.FindByEmailAsync(userLogin.Identifier) ?? await UserManager.FindByNameAsync(userLogin.Identifier);
        if (user is null)
        {
            list.AddUserNotFound(userLogin.Identifier);
            return CreateErrorResult(list);
        }

        Logger.LogInformation("Attempting to log in as user {user} ({userid})", userLogin.Identifier, user.Id);

        var result = await signInManager.CheckPasswordSignInAsync(user, userLogin.Password, false);

        if (result.Succeeded)
        {
            user.RefreshTokenStamp = Guid.NewGuid();
            await Context.SaveChangesAsync();
            var claimsPrincipal = UserSessionReader<TUserInfo, TUserKey>.CreateUserClaimsPrincipal(user, AuthorizationSchemeName);
            await HttpContext.SignInAsync(AuthorizationSchemeName, claimsPrincipal);

            Logger.LogInformation("Succesfully logged in as user {user} ({userid})", userLogin.Identifier, user.Id);

            return CreateResult(HttpContext.Features.Get<GLVAccessTokenResponse>());
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
            return CreateErrorResult(HttpStatusCode.Forbidden);
        }
        else
        {
            Logger.LogInformation("Could not log in as user {user} ({userid})", user.UserName!, user.Id);
            list.AddBadLogin();
            return CreateErrorResult(list);
        }

        list.AddActionDisallowed();
        return CreateErrorResult(list);
    }

    /// <summary>
	/// Logout - 
    /// Logs out the current user
	/// </summary>
    protected internal virtual async Task<IdentityResponse> Logout()
    {
        await HttpContext.SignOutAsync(AuthorizationSchemeName);
        return new(HttpStatusCode.NoContent, null);
    }
}
