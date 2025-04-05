using GLV.Shared.Server.Client.Models;

namespace GLV.Shared.Server.Client;

public interface ISessionStorage
{
    public ValueTask<string?> LoadSession();
    public Task StoreSession(string RefreshToken);
    public Task DeleteSession();
}
