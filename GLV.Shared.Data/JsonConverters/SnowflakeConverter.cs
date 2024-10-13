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
    private SnowflakeConverter() { }

    public static SnowflakeConverter Instance { get; } = new();

    public override Snowflake Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => new(JsonSerializer.Deserialize<long>(ref reader, options));

    public override void Write(Utf8JsonWriter writer, Snowflake value, JsonSerializerOptions options)
        => JsonSerializer.Serialize(writer, value.AsLong(), options);
}
