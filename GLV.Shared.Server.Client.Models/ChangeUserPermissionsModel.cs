namespace GLV.Shared.Server.Client.Models;

public class ChangeUserPermissionsModel
{
    public ulong? NewPermissions { get; set; }
    public uint? NewBaseLevel { get; set; }
}