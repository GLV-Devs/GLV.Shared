using GLV.Shared.ChatBot.Converters;
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace GLV.Shared.ChatBot;

[JsonConverter(typeof(ContextDataSetJsonConverter))]
public sealed class ContextDataSet : IReadOnlyCollection<KeyValuePair<string, ContextData>>
{
    public static JsonConverter<ContextDataSet> JsonConverter { get; } = new ContextDataSetJsonConverter();

    internal readonly Dictionary<string, ContextData> _dict;

    internal ContextDataSet(Dictionary<string, ContextData> dict) => _dict = dict;
    public ContextDataSet() => _dict = [];

    public void Set<T>(string key, T value)
    {
        if (_dict.TryGetValue(key, out var dat) is false || dat is not TypedContextData<T> typedData)
            _dict[key] = new ActualContextData<T>() { Value = value };
        else
            typedData.Value = value;
    }

    public bool ContainsKey(string key) => _dict.ContainsKey(key);

    public bool Remove(string key) => _dict.Remove(key);

    public bool? TryGetValue<T>(string key, [MaybeNullWhen(false)] out T? value)
    {
        if (_dict.TryGetValue(key, out var dat) is false)
        {
            value = default;
            return null;
        }

        if (dat is not TypedContextData<T> typedData)
        {
            value = default;
            return false;
        }

        value = typedData.Value;
        return true;
    }

    public ICollection<string> Keys => _dict.Keys;

    public ICollection<ContextData> Values => _dict.Values;

    public void Clear()
    {
        _dict.Clear();
    }

    public int Count => _dict.Count;

    public IEnumerator<KeyValuePair<string, ContextData>> GetEnumerator()
    {
        return _dict.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return _dict.GetEnumerator();
    }
}
