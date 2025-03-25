namespace GLV.Shared.ChatBot;

public readonly record struct Message(
    string? Text, 
    long MessageId, 
    long? InResponseToMessageId, 
    UserInfo? Sender, 
    bool HasExtraData
);

public readonly record struct UserInfo(string? Username, string? DisplayName, Guid UserId);
public readonly record struct KeyboardKey(string Text, string? Data);
public readonly record struct KeyboardRow(params IEnumerable<KeyboardKey> Keys);
public readonly record struct Keyboard(params IEnumerable<KeyboardRow> Rows);

public readonly record struct KeyboardResponse(string KeyboardId, string? Data)
{
    public bool MatchDataTag(string data)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(data);
        return Data == data;
    }
}

public enum MemberEventKind
{
    MemberJoined,
    MemberLeft,
    MemberKicked,
    Other
}

public readonly record struct MemberEvent(UserInfo Subject, UserInfo? Performer, MemberEventKind Kind);