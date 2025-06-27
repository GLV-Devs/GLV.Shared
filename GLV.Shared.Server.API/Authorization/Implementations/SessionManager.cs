using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Security.Claims;
using System.Security.Cryptography;
using GLV.Shared.Common;
using GLV.Shared.Hosting;
using GLV.Shared.Hosting.Workers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;
using static GLV.Shared.Server.API.Authorization.ISessionManager;

namespace GLV.Shared.Server.API.Authorization.Implementations;

[RegisterService(typeof(SessionManager), Lifetime = ServiceLifetime.Singleton)]
[RegisterService(typeof(ISessionManager), Lifetime = ServiceLifetime.Singleton)]
public class SessionManager : ISessionManager
{
    public SessionManager()
    {

        BackgroundTaskStore.Add(SessionCleanup());
        var klen = GetKeyLength();
        if (klen < 3)
            throw new InvalidOperationException($"KeyLength cannot be less than 3. Value is {klen}");
        KeyLength = klen;
    }

    #region Manager

    public int KeyLength { get; }

    protected virtual int GetKeyLength()
        => 128;

    protected readonly ConcurrentDictionary<string, SessionInfo> SessionStore = new(Environment.ProcessorCount, 100);

    public event SessionDeleted? OnSessionDeleted;

    private Task SessionCleanup() => Task.Run(async () =>
    {
        foreach (var (key, ticket) in SessionStore.ToArray())
        {
            if (ticket.Ticket.Properties.ExpiresUtc > DateTimeOffset.UtcNow)
            {
                SessionStore.TryRemove(key, out _);
                OnSessionDeleted?.Invoke(key, ticket);
            }
        }

        await Task.Delay(TimeSpan.FromMinutes(10));
        BackgroundTaskStore.Add(SessionCleanup());
    });

    public bool TryGenerateNewKey(Span<char> output, out int charsWritten)
    {
        Debug.Assert(KeyLength > 3);
        Span<byte> rand = stackalloc byte[(int)float.Ceiling((((3f * KeyLength) / 4) - 2))];
        RandomNumberGenerator.Fill(rand);
        return Convert.TryToBase64Chars(rand, output, out charsWritten);
    }

    public string GenerateNewKey()
    {
        Span<char> chars = stackalloc char[128];
        var r = TryGenerateNewKey(chars, out _);
        Debug.Assert(r);
        return new string(chars);
    }

    public virtual string CreateNewSession(AuthenticationTicket ticket)
    {
        var key = GenerateNewKey();
        for (int attempts = 0; attempts < 3; attempts++)
        {
            if (SessionStore.TryAdd(key, new(ticket, key)))
                return key;
            key = GenerateNewKey();
        }
        throw new InvalidOperationException("Could not generate a unique session key within 3 attempts");
    }

    public virtual bool TryGetSession(string key, [NotNullWhen(true)] out SessionInfo? session)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        if (key.Length != KeyLength)
        {
            session = null;
            return false;
        }

        return SessionStore.TryGetValue(key, out session);
    }

    public virtual bool DestroySession(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        if (key.Length == KeyLength && SessionStore.TryRemove(key, out var ticket))
        {
            OnSessionDeleted?.Invoke(key, ticket);
            return true;
        }

        return false;
    }

    #endregion
}
