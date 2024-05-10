using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Authentication;

namespace GLV.Shared.Server.API.Authorization;
public interface ISessionManager
{
    public string CreateNewSession(AuthenticationTicket ticket);

    public bool TryGetSession(string key, [NotNullWhen(true)] out AuthenticationTicket? ticket);

    public bool DestroySession(string key);
}