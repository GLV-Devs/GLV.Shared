using System.Collections.Frozen;

namespace GLV.Shared.Server.Identity.Services;

public sealed class UserSessionInfo<TUserInfo, TUserKey>(
    TUserKey requesterUserId, 
    string? username,
    ulong globalPermissions,
    uint userLevel,
    bool refreshable,
    bool isRoot = false
) 
    where TUserInfo : IGlvIdentityUser
    where TUserKey : unmanaged, IEquatable<TUserKey>, IFormattable, IParsable<TUserKey>
{
    public string? UserName { get; } = username;

    public bool Refreshable { get; } = refreshable;

    public TUserKey RequesterUserId { get; } = requesterUserId;

    public ulong GlobalPermissions { get; } = globalPermissions;

    public uint UserLevel { get; } = userLevel;

    public bool IsRoot { get; } = isRoot;

    string? _Str;
    public override string ToString()
        => _Str ??= $"[User:{RequesterUserId}{(IsRoot ? "!Root!" : null)};Permissions:{GlobalPermissions};Level:{UserLevel}{(Refreshable ? ";Refreshable" : null)}]";
}
