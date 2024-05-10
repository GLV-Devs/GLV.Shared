using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using GLV.Shared.Server.API.Workers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;

namespace GLV.Shared.Server.API.Authorization.Implementations;

[RegisterService(typeof(ISessionManager), Lifetime = ServiceLifetime.Singleton)]
public class SessionManager : ISessionManager
{
    public SessionManager()
    {
        BackgroundTaskStore.Add(SessionCleanup, TimeSpan.FromMinutes(20));
    }

    #region Manager

    public const int KeyLength = 128;

    private readonly ConcurrentDictionary<string, AuthenticationTicket> SessionStore = new(Environment.ProcessorCount, 100);

    private Task SessionCleanup(CancellationToken ct) => Task.Run(() =>
    {
        foreach (var (key, ticket) in SessionStore.ToArray())
        {
            if (ticket.Properties.ExpiresUtc is DateTimeOffset exp && exp > DateTimeOffset.Now)
                SessionStore.TryRemove(key, out _);
        }

        BackgroundTaskStore.Add(SessionCleanup, TimeSpan.FromMinutes(10));
    }, ct);

    public bool TryGenerateNewKey(Span<char> output, out int charsWritten)
    {
        Span<Guid> guids = stackalloc Guid[6];
        for (int i = 0; i < guids.Length; i++)
            guids[i] = Guid.NewGuid();
        return Convert.TryToBase64Chars(MemoryMarshal.Cast<Guid, byte>(guids), output, out charsWritten);
    }

    public string GenerateNewKey()
    {
        Span<char> chars = stackalloc char[128];
        var r = TryGenerateNewKey(chars, out _);
        Debug.Assert(r);
        return new string(chars);
    }

    public string CreateNewSession(AuthenticationTicket ticket)
    {
        var key = GenerateNewKey();
        for (int attempts = 0; attempts < 3; attempts++)
        {
            if (SessionStore.TryAdd(key, ticket))
                return key;
            key = GenerateNewKey();
        }
        throw new InvalidOperationException("Could not generate a unique session key within 3 attempts");
    }

    public bool TryGetSession(string key, [NotNullWhen(true)] out AuthenticationTicket? ticket)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        if (key.Length != KeyLength)
        {
            ticket = null;
            return false;
        }

        return SessionStore.TryGetValue(key, out ticket);
    }

    public bool DestroySession(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        return key.Length == KeyLength && SessionStore.TryRemove(key, out _);
    }

    #endregion
}
