using System.Diagnostics.CodeAnalysis;
using System.Security.Claims;
using System.Text.Encodings.Web;
using GLV.Shared.Server.API.Options;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;

namespace GLV.Shared.Server.API.Authorization.Handlers;

public class RandomSessionKeyHandler(IOptions<IdentityOptions> identityOptions, IOptionsMonitor<RandomSessionKeySchemeOptions> optionsMonitor, ISessionManager sessionManager, ILoggerFactory loggerFactory, UrlEncoder urlEncoder)
    : SignInAuthenticationHandler<RandomSessionKeySchemeOptions>(optionsMonitor, loggerFactory, urlEncoder)
{
    public const string SessionKeyHeaderName = "Session";
    protected readonly IdentityOptions IdentityOptions = identityOptions.Value;

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var key = GetRandomSessionKeyOrNull();
        return Task.FromResult(key is null
                || sessionManager.TryGetSession(key, out var ticket) is false
                || ticket.Properties.ExpiresUtc < TimeProvider.GetUtcNow()
            ? AuthenticateResult.NoResult()
            : AuthenticateResult.Success(RefreshTicket(ticket)));
    }

    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.Headers.Append(HeaderNames.WWWAuthenticate, SessionKeyHeaderName);
        return base.HandleChallengeAsync(properties);
    }

    protected override async Task HandleSignInAsync(ClaimsPrincipal user, AuthenticationProperties? properties)
    {
        var utcNow = TimeProvider.GetUtcNow();

        properties ??= new();
        properties.ExpiresUtc = utcNow + Options.RandomSessionKeyExpiration;

        var key = sessionManager.CreateNewSession(CreateTicket(user, properties));
        properties.Items["key"] = key;

        user.AddIdentity(new ClaimsIdentity(new Claim[]
        {
            new(UserAuthConstants.UserLevelClaimsType, "0"),
            new(UserAuthConstants.RefreshableClaimsType, "true")
        }, Scheme.Name));

        Logger.LogInformation("User {user} signed in through scheme {scheme}, session key: {key}", user.Identity!.Name, Scheme.Name, key);

        Context.Features.Set(new SessionKey(key, utcNow, Options.RandomSessionKeyExpiration));
    }

    protected override Task HandleSignOutAsync(AuthenticationProperties? properties)
    {
        var key = properties?.Items["key"];
        if (key is not null)
            sessionManager.DestroySession(key);
        return Task.CompletedTask;
    }

    private string? GetRandomSessionKeyOrNull()
    {
        var authorization = Request.Headers.Authorization.ToString();

        return authorization.StartsWith($"{SessionKeyHeaderName} ", StringComparison.Ordinal)
            ? authorization[$"{SessionKeyHeaderName} ".Length..]
            : null;
    }

    [return: NotNullIfNotNull(nameof(ticket))]
    private AuthenticationTicket? RefreshTicket(AuthenticationTicket? ticket)
    {
        if (ticket is not null)
            ticket.Properties.ExpiresUtc = TimeProvider.GetUtcNow() + Options.RandomSessionKeyExpiration;
        return ticket;
    }

    private AuthenticationTicket CreateTicket(ClaimsPrincipal user, AuthenticationProperties properties)
        => new(user, properties, $"{Scheme.Name}:SessionKey");
}
