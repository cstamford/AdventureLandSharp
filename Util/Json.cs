using System.Text.Json;
using System.Text.Json.Serialization;

namespace AdventureLandSharp.Util;

// aaaaah, typeless languages...

public class JsonBoolOrIntConverter : JsonConverter<bool> {
    public override bool Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => reader.TokenType switch {
        JsonTokenType.True => true,
        JsonTokenType.False => false,
        JsonTokenType.Number when reader.TryGetInt32(out int value) => value != 0,
        _ => throw new JsonException("Value cannot be converted to a boolean.")
    };

    public override void Write(Utf8JsonWriter writer, bool value, JsonSerializerOptions options) => writer.WriteBooleanValue(value);
}

public class JsonBoolOrStringConverter : JsonConverter<bool> {
    public override bool Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => reader.TokenType switch {
        JsonTokenType.True => true,
        JsonTokenType.False => false,
        JsonTokenType.String => true,
        _ => throw new JsonException("Value cannot be converted to a boolean.")
    };

    public override void Write(Utf8JsonWriter writer, bool value, JsonSerializerOptions options) => writer.WriteBooleanValue(value);
}
