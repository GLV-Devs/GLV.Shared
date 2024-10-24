using Microsoft.AspNetCore.Authentication;
using GLV.Shared.Hosting;
using GLV.Shared.Hosting.Workers;

namespace GLV.Shared.Server.API.Options;

[RegisterOptions]
public class RandomSessionKeySchemeOptions : AuthenticationSchemeOptions
{
    public RandomSessionKeySchemeOptions() { }

    public TimeSpan RandomSessionKeyExpiration { get; set; }
}
