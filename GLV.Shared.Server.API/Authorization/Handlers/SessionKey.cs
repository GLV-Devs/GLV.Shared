namespace GLV.Shared.Server.API.Authorization.Handlers;

public class SessionKey(string key, DateTimeOffset issuedDate, TimeSpan expirationTime)
{
    public string Key { get; set; } = key ?? throw new ArgumentNullException(nameof(key));
    public DateTimeOffset IssuedDate { get; set; } = issuedDate;
    public TimeSpan ExpirationTime { get; set; } = expirationTime;
}
