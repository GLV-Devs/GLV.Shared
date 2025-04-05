namespace GLV.Shared.Server.Client.Models;

public interface IGlvIdentityUserLoginModel
{
    public string? Identifier { get; }
    public string? Password { get; set; }
}
