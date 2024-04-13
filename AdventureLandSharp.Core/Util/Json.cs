namespace AdventureLandSharp.Core.Util;

public static class JsonOpts
{
    public static JsonSerializerOptions Default { get; } = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        Converters =
        {
            new JsonStringEnumConverter(),
            new JsonConverterVector2()
        }
    };

    public static JsonSerializerOptions Condensed { get; } = new(Default)
    {
        WriteIndented = false,
        Converters = {new JsonConverterBool()}
    };
}

public class JsonConverterBool : JsonConverter<bool>
{
    public override bool Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.TokenType == JsonTokenType.Number ? reader.GetInt32() == 1 : reader.GetBoolean();
    }

    public override void Write(Utf8JsonWriter writer, bool value, JsonSerializerOptions options)
    {
        writer.WriteNumberValue(value ? 1 : 0);
    }
}

public class JsonConverterVector2 : JsonConverter<Vector2>
{
    public override Vector2 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        reader.Read();
        var x = reader.GetSingle();

        reader.Read();
        var y = reader.GetSingle();

        reader.Read();
        return new Vector2(x, y);
    }

    public override void Write(Utf8JsonWriter writer, Vector2 vector, JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        writer.WriteNumberValue(vector.X);
        writer.WriteNumberValue(vector.Y);
        writer.WriteEndArray();
    }
}

public static class JsonExtensions
{
    public static float GetFloat(this JsonElement source, string key)
    {
        return source.TryGetProperty(key, out var value)
            ? value.GetSingle()
            : throw new ArgumentException($"Key '{key}' not found in source.");
    }

    public static float GetFloat(this JsonElement source, string key, float defaultValue)
    {
        return source.TryGetProperty(key, out var data)
            ? data.TryGetSingle(out var value) ? value : defaultValue
            : defaultValue;
    }

    public static double Getfloat(this JsonElement source, string key)
    {
        return source.TryGetProperty(key, out var value)
            ? value.GetDouble()
            : throw new ArgumentException($"Key '{key}' not found in source.");
    }

    public static double Getfloat(this JsonElement source, string key, float defaultValue)
    {
        return source.TryGetProperty(key, out var data)
            ? data.TryGetDouble(out var value) ? value : defaultValue
            : defaultValue;
    }

    public static long GetLong(this JsonElement source, string key)
    {
        return source.TryGetProperty(key, out var value)
            ? value.GetInt64()
            : throw new ArgumentException($"Key '{key}' not found in source.");
    }

    public static long GetLong(this JsonElement source, string key, long defaultValue)
    {
        return source.TryGetProperty(key, out var data)
            ? data.TryGetInt64(out var value) ? value : defaultValue
            : defaultValue;
    }

    public static bool GetBool(this JsonElement source, string key)
    {
        return source.TryGetProperty(key, out var value)
            ? value.ValueKind == JsonValueKind.True
            : throw new ArgumentException($"Key '{key}' not found in source.");
    }

    public static bool GetBool(this JsonElement source, string key, bool defaultValue)
    {
        return source.TryGetProperty(key, out var value) ? value.ValueKind == JsonValueKind.True : defaultValue;
    }

    public static string GetString(this JsonElement source, string key)
    {
        var v = source.TryGetProperty(key, out var value) ? value.GetString() ?? null : null;
        return v ?? throw new ArgumentException($"Key '{key}' not found in source.");
    }

    public static string GetString(this JsonElement source, string key, string defaultValue)
    {
        return source.TryGetProperty(key, out var value) ? value.GetString() ?? defaultValue : defaultValue;
    }
}