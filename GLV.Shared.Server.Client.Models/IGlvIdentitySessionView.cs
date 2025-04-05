using System.Diagnostics.CodeAnalysis;

namespace GLV.Shared.Server.Client.Models;

public interface IGlvIdentitySessionView
{
    public string IdString { get; }
    public ulong SessionPermissions { get; set; }
    public bool IsRoot { get; }
    public string? UserName { get; set; }
    public uint SessionLevel { get; set; }
}

public class GlvIdentitySessionView<TUserKey> : IGlvIdentitySessionView
    where TUserKey : unmanaged, IEquatable<TUserKey>, IFormattable, IParsable<TUserKey>
{
    [field: AllowNull]
    public string IdString => field ??= Id.ToString()!;
    public TUserKey Id { get; set; }
    public ulong SessionPermissions { get; set; }
    public bool IsRoot { get; init; }
    public string? UserName { get; set; }
    public uint SessionLevel { get; set; }
}