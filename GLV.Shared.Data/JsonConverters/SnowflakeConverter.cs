using GLV.Shared.Data.Identifiers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace GLV.Shared.Data.JsonConverters;
public class SnowflakeConverter : JsonConverter<Snowflake>
{
    private readonly record struct StringAndNumberBuffer(long Value)
    {
        public string ValueString { get; } = Value.ToString();
    }

    private readonly record struct NumberBuffer(long Value);

    public enum ConverterSetting
    {
        EmitStringAndNumberAsObject,
        EmitOnlyNumberAsObject,
        EmitOnlyAsNumber
    }

    public static ConverterSetting Setting { get; set; }

    public SnowflakeConverter() { }

    public static SnowflakeConverter Instance { get; } = new();

    public override Snowflake Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => reader.TokenType == JsonTokenType.Number
            ? new(JsonSerializer.Deserialize<long>(ref reader, options))
            : new(JsonSerializer.Deserialize<NumberBuffer>(ref reader, options).Value);

    public override void Write(Utf8JsonWriter writer, Snowflake value, JsonSerializerOptions options)
    {
        if (Setting is ConverterSetting.EmitStringAndNumberAsObject)
            JsonSerializer.Serialize(writer, new StringAndNumberBuffer(value.AsLong()), options);
        else if (Setting is ConverterSetting.EmitOnlyNumberAsObject)
            JsonSerializer.Serialize(writer, new NumberBuffer(value.AsLong()), options);
        else
            JsonSerializer.Serialize(writer, value.AsLong(), options);
    }
}
