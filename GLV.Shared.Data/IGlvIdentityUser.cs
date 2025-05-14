namespace GLV.Shared.Data;

public interface IGlvIdentityUser 
{
    public string GetIdAsString();
    public ulong Permissions { get; set; }
    public bool IsRoot { get; set; }
    public string? UserName { get; set; }
    public string? Email { get; set; }
    public uint BaseLevel { get; set; }
    public Guid RefreshTokenStamp { get; set; }
}
