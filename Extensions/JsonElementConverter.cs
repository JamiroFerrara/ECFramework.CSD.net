using System;
using System.Text.Json;
using System.Text.Json.Serialization;

public class JsonElementConverter : JsonConverter<object>
{
    public override object Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.TokenType switch
        {
            JsonTokenType.Number => reader.TryGetInt64(out var longValue)
                ? (longValue <= int.MaxValue && longValue >= int.MinValue ? (object)(int)longValue : longValue)
                : reader.GetDouble(),
            JsonTokenType.String => Guid.TryParse(reader.GetString(), out var guid) ? guid : reader.GetString(),
            JsonTokenType.True => true,
            JsonTokenType.False => false,
            _ => JsonDocument.ParseValue(ref reader).RootElement.Clone()
        };
    }
    public override void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value, value?.GetType() ?? typeof(object), options);
    }
}

public class GuidConverter : JsonConverter<Guid>
{
    public override Guid Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => reader.GetGuid();

    public override void Write(Utf8JsonWriter writer, Guid value, JsonSerializerOptions options)
        => writer.WriteStringValue(value);
}
