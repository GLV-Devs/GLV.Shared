using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.AspNetCore.Mvc;
using GLV.Shared.Server.API.Authorization.Handlers;

namespace GLV.Shared.Server.API.Authorization;

/// <summary>
/// Extension methods to configure the GLV bearer token authentication
/// </summary>
public static class GLVBearerTokenExtensions
{
    /// <summary>
    /// Adds bearer token authentication. The default scheme is specified by <see cref="BearerTokenDefaults.AuthenticationScheme"/>.
    /// <para>
    /// Bearer tokens can be obtained by calling <see cref="AuthenticationHttpContextExtensions.SignInAsync(AspNetCore.Http.HttpContext, string?, System.Security.Claims.ClaimsPrincipal)" />.
    /// </para>
    /// </summary>
    /// <param name="builder">The <see cref="AuthenticationBuilder"/>.</param>
    /// <returns>A reference to <paramref name="builder"/> after the operation has completed.</returns>
    public static AuthenticationBuilder AddGLVBearerToken(this AuthenticationBuilder builder)
        => builder.AddGLVBearerToken(BearerTokenDefaults.AuthenticationScheme);

    /// <summary>
    /// Adds bearer token authentication.
    /// <para>
    /// Bearer tokens can be obtained by calling <see cref="AuthenticationHttpContextExtensions.SignInAsync(AspNetCore.Http.HttpContext, string?, System.Security.Claims.ClaimsPrincipal)" />.
    /// </para>
    /// </summary>
    /// <param name="builder">The <see cref="AuthenticationBuilder"/>.</param>
    /// <param name="authenticationScheme">The authentication scheme.</param>
    /// <returns>A reference to <paramref name="builder"/> after the operation has completed.</returns>
    public static AuthenticationBuilder AddGLVBearerToken(this AuthenticationBuilder builder, string authenticationScheme)
        => builder.AddGLVBearerToken(authenticationScheme, _ => { });

    /// <summary>
    /// Adds bearer token authentication. The default scheme is specified by <see cref="BearerTokenDefaults.AuthenticationScheme"/>.
    /// <para>
    /// Bearer tokens can be obtained by calling <see cref="AuthenticationHttpContextExtensions.SignInAsync(AspNetCore.Http.HttpContext, string?, System.Security.Claims.ClaimsPrincipal)" />.
    /// </para>
    /// </summary>
    /// <param name="builder">The <see cref="AuthenticationBuilder"/>.</param>
    /// <param name="configure">Action used to configure the bearer token authentication options.</param>
    /// <returns>A reference to <paramref name="builder"/> after the operation has completed.</returns>
    public static AuthenticationBuilder AddGLVBearerToken(this AuthenticationBuilder builder, Action<BearerTokenOptions> configure)
        => builder.AddGLVBearerToken(BearerTokenDefaults.AuthenticationScheme, configure);

    /// <summary>
    /// Adds bearer token authentication.
    /// <para>
    /// Bearer tokens can be obtained by calling <see cref="AuthenticationHttpContextExtensions.SignInAsync(AspNetCore.Http.HttpContext, string?, System.Security.Claims.ClaimsPrincipal)" />.
    /// </para>
    /// </summary>
    /// <param name="builder">The <see cref="AuthenticationBuilder"/>.</param>
    /// <param name="authenticationScheme">The authentication scheme.</param>
    /// <param name="configure">Action used to configure the bearer token authentication options.</param>
    /// <returns>A reference to <paramref name="builder"/> after the operation has completed.</returns>
    public static AuthenticationBuilder AddGLVBearerToken(this AuthenticationBuilder builder, string authenticationScheme, Action<BearerTokenOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(authenticationScheme);
        ArgumentNullException.ThrowIfNull(configure);

        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IConfigureOptions<BearerTokenOptions>, GLVBearerTokenConfigureOptions>());
        return builder.AddScheme<BearerTokenOptions, GLVBearerTokenHandler>(authenticationScheme, configure);
    }
}
