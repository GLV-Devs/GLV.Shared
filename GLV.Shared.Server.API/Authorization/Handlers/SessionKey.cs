using MessagePack;

namespace GLV.Shared.Server.API.Authorization.Handlers;

[MessagePackObject]
public class SessionKey(string key, DateTimeOffset issuedDate, TimeSpan expirationTime)
{
    [Key(0)]
    public string Key { get; set; } = key ?? throw new ArgumentNullException(nameof(key));

    [Key(1)]
    public DateTimeOffset IssuedDate { get; set; } = issuedDate;

    [Key(2)]
    public TimeSpan ExpirationTime { get; set; } = expirationTime;
}
