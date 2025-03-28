using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GLV.Shared.ChatBot.Converters;

internal sealed class ContextDataSetJsonConverter : JsonConverter<ContextDataSet>
{
    public override ContextDataSet? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        try
        {
            var d = JsonSerializer.Deserialize<Dictionary<string, ContextData>>(ref reader, options);
            return d is null ? null : new(d);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public override void Write(Utf8JsonWriter writer, ContextDataSet value, JsonSerializerOptions options)
        => JsonSerializer.Serialize(writer, value._dict, options);
}

internal sealed class ContextDataJsonConverter : JsonConverter<ContextData>
{
    private static readonly ConcurrentDictionary<string, Type> TypeCache = [];
    private static void RegisterType(Type type)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(type.AssemblyQualifiedName, nameof(type));
        TypeCache.TryAdd(type.AssemblyQualifiedName, type);
    }

    private static Type FetchType(string assemblyQualifiedName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(assemblyQualifiedName, nameof(assemblyQualifiedName));
        if (TypeCache.TryGetValue(assemblyQualifiedName, out var type) is false)
        {
            type = AppDomain.CurrentDomain.GetAssemblies()
                                          .SelectMany(x => x.GetTypes())
                                          .Where(x => x.AssemblyQualifiedName == assemblyQualifiedName)
                                          .SingleOrDefault();
            if (type is null)
                throw new KeyNotFoundException($"Could not find a type by the name of '{assemblyQualifiedName}'");
            TypeCache.TryAdd(assemblyQualifiedName, type);
        }
        return type;
    }

    [ThreadStatic]
    private static Type[]? typeArray;

    public override ContextData? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        string? asmTypeName;

        if (reader.Read() is false
         || reader.TokenType != JsonTokenType.PropertyName
         || string.Equals(reader.GetString(), nameof(ActualContextData<string>.AssemblyQualifiedDataTypeName), StringComparison.OrdinalIgnoreCase) is false
         || reader.Read() is false)
            throw new JsonException("Invalid ContextData json");
        
        if (string.IsNullOrWhiteSpace(asmTypeName = reader.GetString()))
            throw new JsonException("Could not obtain a valid assembly qualified value type name for ContextData");

        var type = FetchType(asmTypeName);
        typeArray ??= new Type[1];
        typeArray[0] = type;

        if (reader.Read() is false
         || reader.TokenType != JsonTokenType.PropertyName
         || string.Equals(reader.GetString(), nameof(ActualContextData<string>.Buffer.Data), StringComparison.OrdinalIgnoreCase) is false
         || reader.Read() is false)
            throw new JsonException("Invalid ContextData json");

        var contextType = typeof(ActualContextData<>).MakeGenericType(typeArray);

        var data = (ContextData?)JsonSerializer.Deserialize(ref reader, contextType, options);

        if (reader.Read() is false
         || reader.TokenType != JsonTokenType.EndObject)
            throw new JsonException("Invalid ContextData json");

        return data;
    }

    public override void Write(Utf8JsonWriter writer, ContextData value, JsonSerializerOptions options)
        => value.SerializeBuffer(writer, options);
}