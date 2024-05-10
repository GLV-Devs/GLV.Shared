using Microsoft.AspNetCore.Authentication;

namespace GLV.Shared.Server.API.Options;

[RegisterOptions]
public class RandomSessionKeySchemeOptions : AuthenticationSchemeOptions
{
    public RandomSessionKeySchemeOptions() { }

    public TimeSpan RandomSessionKeyExpiration { get; set; }
}
