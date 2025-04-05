using System;
using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Security.Claims;
using System.Security.Principal;
using GLV.Shared.Server.API;
using GLV.Shared.Server.API.Authorization;
using GLV.Shared.Server.API.Authorization.Implementations;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace GLV.Shared.Server.Identity.Services;

public class UserSessionReader<TUserInfo, TUserKey>
    where TUserInfo : IGlvIdentityUser
    where TUserKey : unmanaged, IEquatable<TUserKey>, IFormattable, IParsable<TUserKey>
{
    public const string UserSecurityRefreshTokenClaimsType = "GLVSoftworks.Auth.Bearer.RefreshSecurityToken";

    private readonly ConcurrentDictionary<ClaimsPrincipal, UserSessionInfo<TUserInfo, TUserKey>> InfoCache = new();

    public string? AuthorizationSchemeName { get; }

    public UserSessionReader(SessionManager sessionManager, string? authorizationSchemeName = null)
    {
        sessionManager.OnSessionDeleted += SessionManager_OnSessionDeleted;
        AuthorizationSchemeName = authorizationSchemeName;
    }

    private void SessionManager_OnSessionDeleted(string ticketId, SessionInfo session)
    {
        InfoCache.TryRemove(session.Ticket.Principal, out _);
    }

    public UserSessionInfo<TUserInfo, TUserKey>? GetSessionInfo(ClaimsPrincipal principal)
    {
        if (principal.Identity?.IsAuthenticated is not true)
            return null;

        if (InfoCache.TryGetValue(principal, out var info))
            return info;

        TUserKey uid;
        string? username;
        bool isRefreshable;
        uint level;
        bool isRoot = false;
        ulong globalPermissions;

        int valuecount = 0;
        foreach (var claim in principal.Claims)
        {
            if (claim.Type == ClaimTypes.NameIdentifier)
            {
                if (claim.ValueType != ClaimValueTypes.String || TUserKey.TryParse(claim.Value, null, out uid) is false)
                {
                    Debug.Fail("The name identifier claim failed to be parsed; this is likely an internal error");
                    // The purpose of this is that while debugging, all tokens are under our control. However in production, they may not be.
                    // So in debugging, it fails -- While in production, it returns null
                    // Remember that calls to methods under System.Diagnostics.Debug are not emitted when compiled in release mode
                    return null;
                }
                valuecount++;
            }
            else if (claim.Type == ClaimTypes.Name)
            {
                if (claim.ValueType != ClaimValueTypes.String)
                {
                    Debug.Fail("The name identifier claim failed to be parsed; this is likely an internal error");
                    return null;
                }

                username = claim.Value;
                valuecount++;
            }
            else if (claim.Type == UserAuthConstants.RefreshableClaimsType)
            {
                if (claim.ValueType != ClaimValueTypes.Boolean || claim.Value is not "true" and not "false")
                {
                    Debug.Fail("The refreshable claim failed to be parsed; this is likely an internal error");
                    return null;
                }

                isRefreshable = claim.Value is "true";
                valuecount++;
            }
            else if (claim.Type == UserAuthConstants.UserLevelClaimsType)
            {
                if (claim.ValueType != ClaimValueTypes.UInteger32 || uint.TryParse(claim.Value, out level) is false)
                {
                    Debug.Fail("The user level claim failed to be parsed; this is likely an internal error");
                    return null;
                }

                valuecount++;
            }
            else if (claim.Type == UserAuthConstants.UserPermissionClaimsType)
            {
                if (claim.ValueType != ClaimValueTypes.UInteger64
                    || ulong.TryParse(claim.Value, out var perms) is false)
                {
                    Debug.Fail("The user permissions claim failed to be parsed; this is likely an internal error");
                    return null;
                }

                globalPermissions = perms;
                valuecount++;
            }
            else if (claim.Type == UserAuthConstants.IsRootClaimsType)
            {
                if (claim.ValueType != ClaimValueTypes.Boolean || claim.Value is not "true" and not "false")
                {
                    Debug.Fail("The is root claim failed to be parsed; this is likely an internal error");
                    return null;
                }

                isRoot = claim.Value is "true";
            }
        }

        if (valuecount != 5)
        {
            Debug.Fail("The required values count is not exactly 5; this is likely an internal error");
            return null;
        }

        Unsafe.SkipInit(out uid);
        Unsafe.SkipInit(out isRefreshable);
        Unsafe.SkipInit(out level);
        Unsafe.SkipInit(out globalPermissions);
        Unsafe.SkipInit(out username);

        var usi = new UserSessionInfo<TUserInfo, TUserKey>(
            uid,
            username,
            globalPermissions, 
            level, 
            isRefreshable,
            isRoot
        );

        InfoCache[principal] = usi;
        return usi;
    }

    public static ClaimsPrincipal CreateUserClaimsPrincipal(TUserInfo user, string? authorizationSchemeName)
        => new(new ClaimsIdentity[]
        {
            new(GenerateUserClaims(user), authorizationSchemeName)
        });

    public ClaimsPrincipal CreateUserClaimsPrincipal(TUserInfo user)
        => CreateUserClaimsPrincipal(user, AuthorizationSchemeName);

    public static Claim[] GenerateUserClaims(TUserInfo user)
    {
        var arr = new Claim[7];

        arr[0] = new(ClaimTypes.NameIdentifier, user.GetIdAsString(), ClaimValueTypes.String);
        arr[1] = new(ClaimTypes.Name, user.UserName!, ClaimValueTypes.String);
        arr[2] = new(UserAuthConstants.RefreshableClaimsType, "true", ClaimValueTypes.Boolean);
        arr[3] = new(UserAuthConstants.UserLevelClaimsType, "0", ClaimValueTypes.UInteger32);
        arr[4] = new(UserAuthConstants.UserPermissionClaimsType, (user.Permissions).ToString(), ClaimValueTypes.UInteger64);
        arr[5] = new(UserAuthConstants.IsRootClaimsType, user.IsRoot is true ? "true" : "false", ClaimValueTypes.Boolean);
        arr[6] = new(UserSecurityRefreshTokenClaimsType, user.RefreshTokenStamp.ToString(), ClaimValueTypes.String);

        return arr;
    }
}