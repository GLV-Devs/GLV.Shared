using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Authentication;

namespace GLV.Shared.Server.API.Authorization;

public record SessionInfo(AuthenticationTicket Ticket, string SessionId)
{
    public Dictionary<string, object> Data { get; } = [];
};

public interface ISessionManager
{
    public event SessionDeleted? OnSessionDeleted;

    public delegate void SessionDeleted(string ticketId, SessionInfo session);

    public string CreateNewSession(AuthenticationTicket session);

    public bool TryGetSession(string key, [NotNullWhen(true)] out SessionInfo? session);

    public bool DestroySession(string key);
}