namespace GLV.Shared.ChatBot;

public interface IPermissibleActions
{
    public bool BanUsers { get; }
    public bool KickUsers { get; }
    public bool MuteUsers { get; }
    public bool UnmuteUsers { get; }
    public bool DeleteForeignMessages { get; }
}

public sealed class PermissibleActions : IPermissibleActions
{
    public required bool BanUsers { get; set; }
    public required bool KickUsers { get; set; }
    public required bool MuteUsers { get; set; }
    public required bool UnmuteUsers { get; set; }
    public required bool DeleteForeignMessages { get; set; }
}
