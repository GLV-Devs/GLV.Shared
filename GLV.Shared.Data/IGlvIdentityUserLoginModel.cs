namespace GLV.Shared.Data;

public interface IGlvIdentityUserLoginModel
{
    public string? Identifier { get; }
    public string? PasswordSHA256 { get; set; }
}