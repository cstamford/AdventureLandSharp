namespace AdventureLandSharp.Core.Util;

public static class JsonOpts {
    public static JsonSerializerOptions Default { get; } = new() {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        Converters = {
            new JsonStringEnumConverter(),
            new JsonConverterVector2()
        }
    };

    public static JsonSerializerOptions Condensed { get; } = new(Default) {
        WriteIndented = false,
        Converters = { new JsonConverterBool() }
    };
}

public class JsonConverterBool : JsonConverter<bool> {
    public override bool Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        reader.TokenType == JsonTokenType.Number ? reader.GetInt32() == 1 : reader.GetBoolean();

    public override void Write(Utf8JsonWriter writer, bool value, JsonSerializerOptions options) {
        writer.WriteNumberValue(value ? 1 : 0);
    }
}

public class JsonConverterVector2 : JsonConverter<Vector2> {
    public override Vector2 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
        reader.Read();
        float x = reader.GetSingle();

        reader.Read();
        float y = reader.GetSingle();

        reader.Read();
        return new(x, y);
    }

    public override void Write(Utf8JsonWriter writer, Vector2 vector, JsonSerializerOptions options) {
        writer.WriteStartArray();
        writer.WriteNumberValue(vector.X);
        writer.WriteNumberValue(vector.Y);
        writer.WriteEndArray();
    }
}

public static class JsonExtensions {
    public static float GetFloat(this JsonElement source, string key) => source.TryGetProperty(key, out JsonElement value)
        ? value.GetSingle()
        : throw new ArgumentException($"Key '{key}' not found in source.");

    public static float GetFloat(this JsonElement source, string key, float defaultValue) => source.TryGetProperty(key, out JsonElement data)
        ? data.TryGetSingle(out float value) ? value : defaultValue
        : defaultValue;

    public static double Getfloat(this JsonElement source, string key) => source.TryGetProperty(key, out JsonElement value)
        ? value.GetDouble()
        : throw new ArgumentException($"Key '{key}' not found in source.");

    public static double Getfloat(this JsonElement source, string key, float defaultValue) => source.TryGetProperty(key, out JsonElement data)
        ? data.TryGetDouble(out double value) ? value : defaultValue
        : defaultValue;

    public static long GetLong(this JsonElement source, string key) => source.TryGetProperty(key, out JsonElement value)
        ? value.GetInt64()
        : throw new ArgumentException($"Key '{key}' not found in source.");

    public static long GetLong(this JsonElement source, string key, long defaultValue) => source.TryGetProperty(key, out JsonElement data)
        ? data.TryGetInt64(out long value) ? value : defaultValue
        : defaultValue;

    public static bool GetBool(this JsonElement source, string key) => source.TryGetProperty(key, out JsonElement value)
        ? value.ValueKind == JsonValueKind.True
        : throw new ArgumentException($"Key '{key}' not found in source.");

    public static bool GetBool(this JsonElement source, string key, bool defaultValue) =>
        source.TryGetProperty(key, out JsonElement value) ? value.ValueKind == JsonValueKind.True : defaultValue;

    public static string GetString(this JsonElement source, string key) {
        string? v = source.TryGetProperty(key, out JsonElement value) ? value.GetString() ?? null : null;
        return v ?? throw new ArgumentException($"Key '{key}' not found in source.");
    }

    public static string GetString(this JsonElement source, string key, string defaultValue) =>
        source.TryGetProperty(key, out JsonElement value) ? value.GetString() ?? defaultValue : defaultValue;
}
