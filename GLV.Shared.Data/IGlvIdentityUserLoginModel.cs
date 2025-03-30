namespace GLV.Shared.Data;

public interface IGlvIdentityUserLoginModel
{
    public string? Identifier { get; }
    public string? Password { get; set; }
}

public class ChangeUserPasswordModel
{
    public string? OldPassword { get; set; }
    public string? NewPassword { get; set; }
}

public class ChangeUserPermissionsModel<TUserKey>
    where TUserKey : unmanaged, IEquatable<TUserKey>, IFormattable, IParsable<TUserKey>
{
    public TUserKey UserKey { get; set; }
    public ulong? NewPermissions { get; set; }
    public uint? NewBaseLevel { get; set; }
}