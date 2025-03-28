using System.Diagnostics.CodeAnalysis;

namespace GLV.Shared.ChatBot;

public sealed class BotStatusSet()
{
    private readonly Dictionary<string, BotStatus>? _dict;

    public BotStatusSet(Dictionary<string, BotStatus> statusDictionary) : this()
    {
        _dict = statusDictionary;
    }

    public bool TryGetStatus(string key, [NotNullWhen(true)] out BotStatus? status)
    {
        if (_dict is not null && _dict.TryGetValue(key, out status))
            return true;

        status = null;
        return false;
    }

    public ICollection<KeyValuePair<string, BotStatus>> OtherAvailableStatuses 
        => _dict ?? (ICollection<KeyValuePair<string, BotStatus>>)Array.Empty<KeyValuePair<string, BotStatus>>();

    public required BotStatus? Typing { get; init; }
    public required BotStatus? SendingImage { get; init; }
    public required BotStatus? SendingFile { get; init; }
    public required BotStatus? SendingVideo { get; init; }
    public required BotStatus? SendingVoiceMessage { get; init; }
}

public readonly record struct ScopedBotStatusSet(BotStatusSet Set, Guid ScopedConversation)
{
    public bool TryGetStatus(string key, [MaybeNullWhen(false)] out ScopedBotStatus status)
    {
        if (Set.TryGetStatus(key, out var s))
        {
            status = new(s, ScopedConversation);
            return true;
        }

        status = default;
        return false;
    }

    public IEnumerable<KeyValuePair<string, ScopedBotStatus>> OtherAvailableStatuses
    {
        get
        {
            foreach (var (k, v) in Set.OtherAvailableStatuses)
                yield return new(k, new ScopedBotStatus(v, ScopedConversation));
        }
    }

    public ScopedBotStatus Typing => new(Set.Typing, ScopedConversation);
    public ScopedBotStatus SendingImage => new(Set.SendingImage, ScopedConversation);
    public ScopedBotStatus SendingFile => new(Set.SendingFile, ScopedConversation);
    public ScopedBotStatus SendingVideo => new(Set.SendingVideo, ScopedConversation);
    public ScopedBotStatus SendingVoiceMessage => new(Set.SendingVoiceMessage, ScopedConversation);
}
