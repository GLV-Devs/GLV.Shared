using GLV.Shared.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using GLV.Shared.Server.Client.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using GLV.Shared.Server.Identity.Controllers;
using GLV.Shared.Server.API.Authorization.Implementations;
using GLV.Shared.Server.Identity.Services;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.Extensions.Options;
using System;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using GLV.Shared.DataTransfer;
using System.Net;

namespace GLV.Shared.Server.Identity;

public readonly record struct IdentityResponse(HttpStatusCode Status, IServerResponse? Data);

public static class GlvEndpointExtensions
{
    public static IServiceCollection AddGlvIdentityServices<TUserInfo, TUserKey>(this IServiceCollection services, string? authorizationSchemeName = null)
        where TUserInfo : class, IKeyed<TUserInfo, TUserKey>, IGlvIdentityUser
        where TUserKey : unmanaged, IEquatable<TUserKey>, IFormattable, IParsable<TUserKey>
    {
        services.TryAddScoped<UserSessionReader<TUserInfo, TUserKey>>(s => new(s.GetRequiredService<SessionManager>(), authorizationSchemeName));

        services.AddHttpContextAccessor();
        services.TryAddScoped<IUserValidator<TUserInfo>, UserValidator<TUserInfo>>();
        services.TryAddScoped<IPasswordValidator<TUserInfo>, PasswordValidator<TUserInfo>>();
        services.TryAddScoped<IPasswordHasher<TUserInfo>, PasswordHasher<TUserInfo>>();
        services.TryAddScoped<ILookupNormalizer, UpperInvariantLookupNormalizer>();

        services.TryAddScoped<IdentityErrorDescriber>();
        services.TryAddScoped<ISecurityStampValidator, SecurityStampValidator<TUserInfo>>();
        services.TryAddScoped<ITwoFactorSecurityStampValidator, TwoFactorSecurityStampValidator<TUserInfo>>();
        services.TryAddScoped<IUserConfirmation<TUserInfo>, DefaultUserConfirmation<TUserInfo>>();
        services.TryAddScoped<UserManager<TUserInfo>>();
        services.TryAddScoped<SignInManager<TUserInfo>>();

        return services;
    }

    public static IServiceCollection AddGlvIdentityControllerServices<
        TUserInfo,
        TContext,
        TUserKey,
        TUserView,
        TUserCreateModel,
        TUserUpdateModel,
        TUserLoginModel,
        TUserSessionView,
        TController
    >
    (this IServiceCollection services, string baseRoute = "api/identity", string? authorizationSchemeName = null)
        where TContext : DbContext
        where TUserInfo : class, IKeyed<TUserInfo, TUserKey>, IGlvIdentityUser
        where TUserKey : unmanaged, IEquatable<TUserKey>, IFormattable, IParsable<TUserKey>
        where TUserLoginModel : IGlvIdentityUserLoginModel
        where TUserSessionView : IGlvIdentitySessionView
        where TController : GlvIdentityController<TUserInfo, TContext, TUserKey, TUserView, TUserCreateModel, TUserUpdateModel, TUserLoginModel, TUserSessionView>
    {
        services.AddGlvIdentityServices<TUserInfo, TUserKey>(authorizationSchemeName);
        services.TryAddScoped<GlvIdentityController<TUserInfo, TContext, TUserKey, TUserView, TUserCreateModel, TUserUpdateModel, TUserLoginModel, TUserSessionView>, TController>();

        return services;
    }

    public static WebApplication AddGlvIdentityEndpoints<
        TUserInfo,
        TContext,
        TUserKey,
        TUserView,
        TUserCreateModel,
        TUserUpdateModel,
        TUserLoginModel,
        TUserSessionView
    > 
    (
        this WebApplication app, 
        string baseRoute = "api/identity", 
        string authorizationSchemeName = BearerTokenDefaults.AuthenticationScheme, 
        string endpointTag = "GLV Media"
    )
        where TContext : DbContext
        where TUserInfo : class, IKeyed<TUserInfo, TUserKey>, IGlvIdentityUser
        where TUserKey : unmanaged, IEquatable<TUserKey>, IFormattable, IParsable<TUserKey>
        where TUserLoginModel : IGlvIdentityUserLoginModel
        where TUserSessionView : IGlvIdentitySessionView
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(authorizationSchemeName);
        ArgumentException.ThrowIfNullOrWhiteSpace(baseRoute);

        List<string> authSchemes = [authorizationSchemeName];

        app.MapPut(baseRoute, Login).WithTags(endpointTag);
        app.MapPost(baseRoute, Create).WithTags(endpointTag);
        app.MapPatch($"{baseRoute}/password", ChangePassword).RequireAuthorization(ConfigurePolicy).WithTags(endpointTag);
        app.MapPatch($"{baseRoute}/permissions/{{userId}}", ChangePermissions).RequireAuthorization(ConfigurePolicy).WithTags(endpointTag);
        app.MapPatch($"{baseRoute}/refresh", Refresh).RequireAuthorization(ConfigurePolicy).WithTags(endpointTag);
        app.MapGet(baseRoute, GetSessionInfo).RequireAuthorization(ConfigurePolicy).WithTags(endpointTag);
        app.MapDelete(baseRoute, (Delegate)Logout).RequireAuthorization(ConfigurePolicy).WithTags(endpointTag);

        return app;

        // This looks ugly as HELL because it's a long class. But it's just a local function
        GlvIdentityController<TUserInfo, TContext, TUserKey, TUserView, TUserCreateModel, TUserUpdateModel, TUserLoginModel, TUserSessionView> 
            GetController(HttpContext context)
        {
            var actionContext = new ActionContext(context, context.GetRouteData(), new ControllerActionDescriptor());
            var controller = context.RequestServices
                .GetRequiredService<
                    GlvIdentityController<TUserInfo, TContext, TUserKey, TUserView, TUserCreateModel, TUserUpdateModel, TUserLoginModel, TUserSessionView>
                >();

            controller.AuthorizationSchemeName = authorizationSchemeName;
            controller.UserManager = context.RequestServices.GetRequiredService<UserManager<TUserInfo>>();
            controller.Context = context.RequestServices.GetRequiredService<TContext>();
            controller.ControllerContext = new ControllerContext(actionContext);

            return controller;
        }

        async Task Create(TUserCreateModel creationModel, HttpContext context)
        {
            var controller = GetController(context);
            await FlushResponse(await controller.Create(creationModel), controller);
        }

        async Task ChangePassword(ChangeUserPasswordModel model, HttpContext context)
        {
            var controller = GetController(context);
            await FlushResponse(await controller.ChangePassword(model), controller);
        }

        async Task ChangePermissions(TUserKey userId, ChangeUserPermissionsModel model, HttpContext context)
        {
            var controller = GetController(context);
            await FlushResponse(await controller.ChangePermissions(userId, model), controller);
        }

        Task GetSessionInfo(HttpContext context)
        {
            var controller = GetController(context);
            return FlushResponse(controller.GetSessionInfo(), controller);
        }

        async Task Login(TUserLoginModel? userLogin, HttpContext context)
        {
            var controller = GetController(context);
            await FlushResponse(await controller.Login(userLogin), controller);
        }

        async Task Logout(HttpContext context)
        {
            var controller = GetController(context);
            await FlushResponse(await controller.Logout(), controller);
        }

        async Task Refresh(RefreshRequest request, HttpContext context)
        {
            var controller = GetController(context);
            var bearerTokenOptions = context.RequestServices.GetRequiredService<IOptionsMonitor<BearerTokenOptions>>();
            var timeProvider = context.RequestServices.GetRequiredService<TimeProvider>();
            await FlushResponse(await controller.Refresh(request, bearerTokenOptions, timeProvider), controller);
        }

        async Task FlushResponse(IdentityResponse result, ControllerBase controller)
        {
            controller.Response.StatusCode = (int)result.Status;
            if (result.Data is not null)
                await controller.Response.WriteAsJsonAsync(result.Data, result.Data.GetType());
            
            await controller.Response.CompleteAsync();
        }

        void ConfigurePolicy(AuthorizationPolicyBuilder b)
        {
            b.AuthenticationSchemes = authSchemes;
            b.RequireAuthenticatedUser();
        }
    }
}
