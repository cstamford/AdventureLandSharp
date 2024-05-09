using System.Diagnostics;
using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AdventureLandSharp.Core.Util;

public static class JsonOpts {
    public static JsonSerializerOptions Default => new JsonSerializerOptions() {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    }
        .AddStringEnumConverter()
        .AddVector2Converter();

    public static JsonSerializerOptions Condensed => new JsonSerializerOptions(Default) { 
        WriteIndented = false
    }
        .AddBoolConverter();

    public static JsonSerializerOptions AddStringEnumConverter(this JsonSerializerOptions options) {
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }

    public static JsonSerializerOptions AddVector2Converter(this JsonSerializerOptions options) {
        options.Converters.Add(new JsonConverterVector2());
        return options;
    }

    public static JsonSerializerOptions AddMapLocationConverter(this JsonSerializerOptions options, World world) {
        options.Converters.Add(new JsonConverterMapLocation(world));
        return options;
    }

    public static JsonSerializerOptions AddArrayOrFalseConverter<T>(this JsonSerializerOptions options) {
        options.Converters.Add(new JsonConverterArrayOrFalse<T>());
        return options;
    }

    public static JsonSerializerOptions AddBoolConverter(this JsonSerializerOptions options) {
        options.Converters.Add(new JsonConverterBool());
        return options;
    }
}

public class JsonConverterBool : JsonConverter<bool> {
    public override bool Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => 
        reader.TokenType == JsonTokenType.Number ? reader.GetInt32() == 1 : reader.GetBoolean();
    
    public override void Write(Utf8JsonWriter writer, bool value, JsonSerializerOptions options) =>
        writer.WriteNumberValue(value ? 1 : 0);
}

public class JsonConverterVector2 : JsonConverter<Vector2> {
    public override Vector2 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
        Debug.Assert(reader.TokenType == JsonTokenType.StartArray);
        reader.Read();

        float x = reader.GetSingle();
        reader.Read();

        float y = reader.GetSingle();
        reader.Read();

        Debug.Assert(reader.TokenType == JsonTokenType.EndArray);
        return new(x, y);
    }

    public override void Write(Utf8JsonWriter writer, Vector2 vector, JsonSerializerOptions options) {
        writer.WriteStartArray();
        writer.WriteNumberValue(vector.X);
        writer.WriteNumberValue(vector.Y);
        writer.WriteEndArray();
    }
}

public class JsonConverterArrayOrFalse<T> : JsonConverter<T[]> {
    public override T[]? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => 
        reader.TokenType == JsonTokenType.False ? [] : JsonSerializer.Deserialize<T[]>(ref reader, options);

    public override void Write(Utf8JsonWriter writer, T[] value, JsonSerializerOptions options) =>
        JsonSerializer.Serialize(writer, value, options);
}

public class JsonConverterMapLocation(World world) : JsonConverter<MapLocation> {
    public override MapLocation Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
        Debug.Assert(reader.TokenType == JsonTokenType.StartArray);
        reader.Read();

        string mapName = reader.GetString()!;
        reader.Read();

        float x = reader.GetSingle();
        reader.Read();

        float y = reader.GetSingle();
        reader.Read();

        Debug.Assert(reader.TokenType == JsonTokenType.EndArray);
        return new(world.GetMap(mapName), new(x, y));
    }

    public override void Write(Utf8JsonWriter writer, MapLocation location, JsonSerializerOptions options) {
        writer.WriteStartArray();
        writer.WriteStringValue(location.Map.Name);
        writer.WriteNumberValue(location.Position.X);
        writer.WriteNumberValue(location.Position.Y);
        writer.WriteEndArray();
    }
}

public static class JsonExtensions {
    public static float GetFloat(this JsonElement source, string key) {
        return source.TryGetProperty(key, out JsonElement value) ? value.GetSingle() : throw new ArgumentException($"Key '{key}' not found in source.");
    }

    public static float GetFloat(this JsonElement source, string key, float defaultValue) {
        return source.TryGetProperty(key, out JsonElement data) ? data.TryGetSingle(out float value) ? value : defaultValue : defaultValue;
    }

    public static double GetDouble(this JsonElement source, string key) {
        return source.TryGetProperty(key, out JsonElement value) ? value.GetDouble() : throw new ArgumentException($"Key '{key}' not found in source.");
    }

    public static double GetDouble(this JsonElement source, string key, float defaultValue) {
        return source.TryGetProperty(key, out JsonElement data) ? data.TryGetDouble(out double value) ? value : defaultValue : defaultValue;
    }

    public static int GetInt(this JsonElement source, string key) {
        return source.TryGetProperty(key, out JsonElement value) ? value.GetInt32() : throw new ArgumentException($"Key '{key}' not found in source.");
    }

    public static int GetInt(this JsonElement source, string key, int defaultValue) {
        return source.TryGetProperty(key, out JsonElement data) ? data.TryGetInt32(out int value) ? value : defaultValue : defaultValue;
    }

    public static long GetLong(this JsonElement source, string key) {
        return source.TryGetProperty(key, out JsonElement value) ? value.GetInt64() : throw new ArgumentException($"Key '{key}' not found in source.");
    }

    public static long GetLong(this JsonElement source, string key, long defaultValue) {
        return source.TryGetProperty(key, out JsonElement data) ? data.TryGetInt64(out long value) ? value : defaultValue : defaultValue;
    }

    public static bool GetBool(this JsonElement source, string key) {
        return source.TryGetProperty(key, out JsonElement value) ? value.ValueKind == JsonValueKind.True : throw new ArgumentException($"Key '{key}' not found in source.");
    }

    public static bool GetBool(this JsonElement source, string key, bool defaultValue) {
        return source.TryGetProperty(key, out JsonElement value) ? value.ValueKind == JsonValueKind.True : defaultValue;
    }

    public static string GetString(this JsonElement source, string key) {
        string? v = source.TryGetProperty(key, out JsonElement value) ? value.GetString() ?? null : null;
        return v ?? throw new ArgumentException($"Key '{key}' not found in source.");
    }

    public static string GetString(this JsonElement source, string key, string defaultValue) {
        return source.TryGetProperty(key, out JsonElement value) ? value.GetString() ?? defaultValue : defaultValue;
    }
}
