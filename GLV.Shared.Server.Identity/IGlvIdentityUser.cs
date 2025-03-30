namespace GLV.Shared.Server.Identity;

public interface IGlvIdentityUser
{
    public string GetIdAsString();
    public ulong Permissions { get; set; }
    public bool IsRoot { get; }
    public Guid RefreshTokenStamp { get; set; }
    public string? UserName { get; set; }
    public uint BaseLevel { get; set; }
}
