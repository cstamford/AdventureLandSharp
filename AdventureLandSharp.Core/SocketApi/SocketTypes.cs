using System.Numerics;
using System.Text.Json;

namespace AdventureLandSharp.Core.SocketApi;

public enum SocketEntityType { Player, Monster }

public readonly record struct SocketItem(string Name, long Level) {
    public SocketItem(JsonElement source) : this(
        source.GetProperty("name").GetString()!,
        source.TryGetProperty("level", out JsonElement level) ? level.GetInt64() : 0)
    { } 
}

public readonly record struct SocketInventoryItem(SocketItem Item, int SlotId, long Quantity, object? SpecialType) {
    public SocketInventoryItem(JsonElement slot, int slotId) : this(
        new SocketItem(slot),
        slotId,
        slot.TryGetProperty("q", out JsonElement q) ? q.GetInt64() : 1,
        slot.TryGetProperty("p", out JsonElement p) ? p : null) 
    { }
}

public readonly record struct SocketPlayerInventory(double Gold, IReadOnlyList<SocketInventoryItem> Items) {
    public SocketPlayerInventory(JsonElement source) : this(
        source.GetProperty("gold").GetDouble(),
        [..source.GetProperty("slots").EnumerateArray().Select((x, i) => new SocketInventoryItem(x, i))])
    { }
}

public readonly record struct SocketDropData(string Id, double X, double Y);

public readonly record struct SocketPlayerEquipment(
    SocketItem? Ring1,
    SocketItem? Ring2,
    SocketItem? Earring1,
    SocketItem? Earring2,
    SocketItem? Belt,
    SocketItem? MainHand,
    SocketItem? OffHand,
    SocketItem? Helmet,
    SocketItem? Chest,
    SocketItem? Pants,
    SocketItem? Shoes,
    SocketItem? Gloves,
    SocketItem? Amulet,
    SocketItem? Orb,
    SocketItem? Elixir,
    SocketItem? Cape)
{
    public SocketPlayerEquipment(JsonElement source) : this(
        source.TryGetProperty("ring1", out JsonElement ring1) && ring1.ValueKind != JsonValueKind.Null ? new SocketItem(ring1) : null,
        source.TryGetProperty("ring2", out JsonElement ring2) && ring2.ValueKind != JsonValueKind.Null ? new SocketItem(ring2) : null,
        source.TryGetProperty("earring1", out JsonElement earring1) && earring1.ValueKind != JsonValueKind.Null ? new SocketItem(earring1) : null,
        source.TryGetProperty("earring2", out JsonElement earring2) && earring2.ValueKind != JsonValueKind.Null ? new SocketItem(earring2) : null,
        source.TryGetProperty("belt", out JsonElement belt) && belt.ValueKind != JsonValueKind.Null ? new SocketItem(belt) : null,
        source.TryGetProperty("mainhand", out JsonElement mainhand) && mainhand.ValueKind != JsonValueKind.Null ? new SocketItem(mainhand) : null,
        source.TryGetProperty("offhand", out JsonElement offhand) && offhand.ValueKind != JsonValueKind.Null ? new SocketItem(offhand) : null,
        source.TryGetProperty("helmet", out JsonElement helmet) && helmet.ValueKind != JsonValueKind.Null ? new SocketItem(helmet) : null,
        source.TryGetProperty("chest", out JsonElement chest) && chest.ValueKind != JsonValueKind.Null ? new SocketItem(chest) : null,
        source.TryGetProperty("pants", out JsonElement pants) && pants.ValueKind != JsonValueKind.Null ? new SocketItem(pants) : null,
        source.TryGetProperty("shoes", out JsonElement shoes) && shoes.ValueKind != JsonValueKind.Null ? new SocketItem(shoes) : null,
        source.TryGetProperty("gloves", out JsonElement gloves) && gloves.ValueKind != JsonValueKind.Null ? new SocketItem(gloves) : null,
        source.TryGetProperty("amulet", out JsonElement amulet) && amulet.ValueKind != JsonValueKind.Null ? new SocketItem(amulet) : null,
        source.TryGetProperty("orb", out JsonElement orb) && orb.ValueKind != JsonValueKind.Null ? new SocketItem(orb) : null,
        source.TryGetProperty("elixir", out JsonElement elixir) && elixir.ValueKind != JsonValueKind.Null ? new SocketItem(elixir) : null,
        source.TryGetProperty("cape", out JsonElement cape) && cape.ValueKind != JsonValueKind.Null ? new SocketItem(cape) : null)
    { }
}

public readonly record struct SocketStatusEffect(
    string Name,
    string Originator,
    double Remaining
);

public readonly record struct SocketEntityData(
    SocketEntityType Type,
    string TypeString,
    string Id,
    string Map,
    double Level,
    string Target,
    bool Moving,
    Vector2 Position,
    Vector2? TargetPosition,
    double Hp,
    double MaxHp,
    double Mp,
    double MaxMp,
    double Speed,
    double Attack,
    double AttackRange,
    double AttackFrequency,
    double Xp,
    double Armour,
    double Resistance,
    IReadOnlyList<SocketStatusEffect> StatusEffects,
    bool Dead
);

public class SocketEntity {
    public SocketEntityType Type { get; init; }
    public string TypeString { get; set; } = string.Empty;
    public string Id { get; set; } = string.Empty;
    public string Map { get; set; } = string.Empty;
    public long MapId { get; set; } = -1;
    public double Level { get; set; }
    public string Target { get; set; } = string.Empty;
    public bool Moving { get; set; }
    public Vector2 Position { get; set; }
    public Vector2? TargetPosition { get; set; }
    public double Hp { get; set; }
    public double MaxHp { get; set; }
    public double Mp { get; set; }
    public double MaxMp { get; set; }
    public double Speed { get; set; }
    public double Attack { get; set; }
    public double AttackRange { get; set; }
    public double AttackFrequency { get; set; }
    public double Xp { get; set; }
    public double Armour { get; set; }
    public double Resistance { get; set; }
    public List<SocketStatusEffect> StatusEffects { get; set; } = [];
    public bool Dead { get; set; }

    public void Update(Dictionary<string, JsonElement> source, GameDataMonster? monsterDef = null) {
        Id = GetValueOrDefault(source, "id", Id);
        Map = GetValueOrDefault(source, "map", Map);
        MapId = GetValueOrDefault(source, "m", MapId);
        Target = GetValueOrDefault(source, "target", string.Empty);
        Level = GetValueOrDefault(source, "level", Level);
        Moving = GetValueOrDefault(source, "moving", true);
        Position = new(
            source["x"].GetSingle(),
            source["y"].GetSingle());
        TargetPosition = new(
            GetValueOrDefault(source, "going_x", Position.X),
            GetValueOrDefault(source, "going_y", Position.Y));
        Speed = GetValueOrDefault(source, "speed", monsterDef?.Speed ?? Speed);
        Hp = GetValueOrDefault(source, "hp", monsterDef?.Hp ?? Hp);
        MaxHp = GetValueOrDefault(source, "max_hp", monsterDef?.Hp ?? MaxMp);
        Mp = GetValueOrDefault(source, "mp", monsterDef?.Mp ?? Mp);
        MaxMp = GetValueOrDefault(source, "max_mp", monsterDef?.Mp ?? MaxMp);
        Attack = GetValueOrDefault(source, "attack", monsterDef?.Attack ?? Attack);
        AttackRange = GetValueOrDefault(source, "range", monsterDef?.Range ?? AttackRange);
        AttackFrequency = GetValueOrDefault(source, "frequency", monsterDef?.Frequency ?? AttackFrequency);
        Xp = GetValueOrDefault(source, "xp", monsterDef?.Xp ?? Xp);
        Armour = GetValueOrDefault(source, "armor", Armour);
        Resistance = GetValueOrDefault(source, "resistance", Resistance);

        if (source.TryGetValue("s", out JsonElement statusEffectsElement)) {
            StatusEffects = statusEffectsElement
                .EnumerateObject()
                .Select(statusEffect => new SocketStatusEffect(
                    Name: statusEffect.Name,
                    Originator: statusEffect.Value.TryGetProperty("f", out JsonElement f) ? f.GetString() ?? string.Empty : string.Empty,
                    Remaining: statusEffect.Value.GetProperty("ms").GetDouble()))
                .ToList();
        }

        Dead = GetValueOrDefault(source, "rip", false);
    }

    public SocketEntityData Data => new(
        Type: Type,
        TypeString: TypeString,
        Id: Id,
        Map: Map,
        Level: Level,
        Target: Target,
        Moving: Moving,
        Position: Position,
        TargetPosition: TargetPosition,
        Hp: Hp,
        MaxHp: MaxHp,
        Mp: Mp,
        MaxMp: MaxMp,
        Speed: Speed,
        Attack: Attack,
        AttackRange: AttackRange,
        AttackFrequency: AttackFrequency,
        Xp: Xp,
        Armour: Armour,
        Resistance: Resistance,
        StatusEffects: StatusEffects,
        Dead: Dead
    );

    private static float GetValueOrDefault(Dictionary<string, JsonElement> source, string key, float defaultValue) {
        return source.TryGetValue(key, out JsonElement data) ? data.TryGetSingle(out float value) ? value : defaultValue : defaultValue;
    }

    private static double GetValueOrDefault(Dictionary<string, JsonElement> source, string key, double defaultValue) {
        return source.TryGetValue(key, out JsonElement data) ? data.TryGetDouble(out double value) ? value : defaultValue : defaultValue;
    }

    private static long GetValueOrDefault(Dictionary<string, JsonElement> source, string key, long defaultValue) {
        return source.TryGetValue(key, out JsonElement data) ? data.TryGetInt64(out long value) ? value : defaultValue : defaultValue;
    }

    private static bool GetValueOrDefault(Dictionary<string, JsonElement> source, string key, bool defaultValue) {
        return source.TryGetValue(key, out JsonElement value) ? value.ValueKind == JsonValueKind.True : defaultValue;
    }

    private static string GetValueOrDefault(Dictionary<string, JsonElement> source, string key, string defaultValue) {
        return source.TryGetValue(key, out JsonElement value) ? value.GetString() ?? defaultValue : defaultValue;
    }
}
