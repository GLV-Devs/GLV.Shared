using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.DataProtection;

namespace GLV.Shared.Server.API.Authorization;

public sealed class GLVBearerTokenConfigureOptions(IDataProtectionProvider dp) : IConfigureNamedOptions<BearerTokenOptions>
{
    private const string _primaryPurpose = "Microsoft.AspNetCore.Authentication.BearerToken";

    public void Configure(string? schemeName, BearerTokenOptions options)
    {
        if (schemeName is null)
        {
            return;
        }

        options.BearerTokenProtector = new TicketDataFormat(dp.CreateProtector(_primaryPurpose, schemeName, "BearerToken"));
        options.RefreshTokenProtector = new TicketDataFormat(dp.CreateProtector(_primaryPurpose, schemeName, "RefreshToken"));
    }

    public void Configure(BearerTokenOptions options)
    {
        throw new NotImplementedException();
    }
}
