using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AdventureLandSharp.Core;

public static class GameConstants {
    public const int VisionWidth = 700;
    public const int VisionHeight = 500;
    public const int SellDist = 400;
    public const int LootDist = 400;
    public const int DoorDist = 112;
    public const int TransporterDist = 160;
}

public readonly record struct GameLevelGeometry(
    [property: JsonPropertyName("default")] int Default,
    [property: JsonPropertyName("min_x")] int MinX,
    [property: JsonPropertyName("min_y")] int MinY,
    [property: JsonPropertyName("max_x")] int MaxX,
    [property: JsonPropertyName("max_y")] int MaxY,
    [property: JsonPropertyName("x_lines")] List<int[]>? XLines,
    [property: JsonPropertyName("y_lines")] List<int[]>? YLines,
    [property: JsonPropertyName("tiles")] List<JsonElement[]>? Tiles,
    [property: JsonPropertyName("placements")] List<int[]>? Placements,
    [property: JsonPropertyName("groups")] List<object>? Groups,
    [property: JsonPropertyName("animations")] List<object>? Animations
);

public readonly record struct GameItemUpgrade(
    [property: JsonPropertyName("range")] double Range,
    [property: JsonPropertyName("attack")] double Attack
);

public readonly record struct GameItemCx(
    [property: JsonPropertyName("scale")] double Scale,
    [property: JsonPropertyName("extension")] bool Extension
);

public readonly record struct GameItem(
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("wtype")] string WType,
    [property: JsonPropertyName("tier")] double Tier,
    [property: JsonPropertyName("skin")] string Skin,
    [property: JsonPropertyName("damage_type")] string DamageType,
    [property: JsonPropertyName("upgrade")] GameItemUpgrade Upgrade,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("g")] double Gold,
    [property: JsonPropertyName("grades")] List<double> Grades,
    [property: JsonPropertyName("range")] double Range,
    [property: JsonPropertyName("attack")] double Attack,
    [property: JsonPropertyName("cx")] GameItemCx Cx
);

public readonly record struct GameDataMonster(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("speed")] double Speed,
    [property: JsonPropertyName("charge")] double Charge,
    [property: JsonPropertyName("hp")] double Hp,
    [property: JsonPropertyName("xp")] double Xp,
    [property: JsonPropertyName("attack")] double Attack,
    [property: JsonPropertyName("damage_type")] string DamageType,
    [property: JsonPropertyName("respawn")] double Respawn,
    [property: JsonPropertyName("range")] double Range,
    [property: JsonPropertyName("frequency")] double Frequency,
    [property: JsonPropertyName("aggro")] double Aggro,
    [property: JsonPropertyName("aa")] double Aa,
    [property: JsonPropertyName("achievements")] List<List<object>> Achievements,
    [property: JsonPropertyName("skin")] string Skin,
    [property: JsonPropertyName("rage")] double Rage,
    [property: JsonPropertyName("mp")] double Mp
);

public readonly record struct GameDataNpc(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("places")] Dictionary<string, long>? Places
);

public readonly record struct GameDataMap(
    [property: JsonPropertyName("key")] string Key,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("monsters")] GameDataMapMonster[]? Monsters,
    [property: JsonPropertyName("npcs")] GameDataMapNpc[] Npcs,
    [property: JsonPropertyName("spawns")] double[][] SpawnPositions, // [n] spawn ID, [n][0] x, [n][1] y
    [property: JsonPropertyName("doors")] JsonElement[][] Doors // [n][0] x, [1] y, [4] destination map, [5] destination spawn ID
);

public readonly record struct GameDataMapMonster(
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("boundary")] double[]? Boundary,
    [property: JsonPropertyName("boundaries")] JsonElement[][]? Boundaries,
    [property: JsonPropertyName("position")] double[]? Position,
    [property: JsonPropertyName("radius")] double? Radius,
    [property: JsonPropertyName("count")] double Count
) {
    public Vector2 GetSpawnPosition() {
        if (Boundary is { Length: 4 }) {
            return new((float)(Boundary[0] + Boundary[2]) / 2, (float)(Boundary[1] + Boundary[3]) / 2);
        }

        if (Position is { Length: 2 }) {
            return new((float)Position[0], (float)Position[1]);
        }

        throw new("No spawn position available.");
    }
};

public readonly record struct GameDataMapNpc(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("position")] double[] Position
);

public readonly record struct GameData(
    [property: JsonPropertyName("positions")] Dictionary<string, object> Positions,
    [property: JsonPropertyName("titles")] Dictionary<string, object> Titles,
    [property: JsonPropertyName("tilesets")] Dictionary<string, object> Tilesets,
    [property: JsonPropertyName("images")] Dictionary<string, object> Images,
    [property: JsonPropertyName("imagesets")] Dictionary<string, object> Imagesets,
    [property: JsonPropertyName("dimensions")] Dictionary<string, double[]> Dimensions,
    [property: JsonPropertyName("emotions")] Dictionary<string, object> Emotions,
    [property: JsonPropertyName("multipliers")] Dictionary<string, object> Multipliers,
    [property: JsonPropertyName("maps")] Dictionary<string, GameDataMap> Maps,
    [property: JsonPropertyName("version")] int Version,
    [property: JsonPropertyName("cosmetics")] Dictionary<string, object> Cosmetics,
    [property: JsonPropertyName("conditions")] Dictionary<string, object> Conditions,
    [property: JsonPropertyName("monsters")] Dictionary<string, GameDataMonster> Monsters,
    [property: JsonPropertyName("achievements")] Dictionary<string, object> Achievements,
    [property: JsonPropertyName("docs")] Dictionary<string, object> Docs,
    [property: JsonPropertyName("dismantle")] Dictionary<string, object> Dismantle,
    [property: JsonPropertyName("projectiles")] Dictionary<string, object> Projectiles,
    [property: JsonPropertyName("tokens")] Dictionary<string, object> Tokens,
    [property: JsonPropertyName("craft")] Dictionary<string, object> Craft,
    [property: JsonPropertyName("animations")] Dictionary<string, object> Animations,
    [property: JsonPropertyName("npcs")] Dictionary<string, GameDataNpc> Npcs,
    [property: JsonPropertyName("geometry")] Dictionary<string, GameLevelGeometry> Geometry,
    [property: JsonPropertyName("items")] Dictionary<string, GameItem> Items,
    [property: JsonPropertyName("levels")] Dictionary<string, object> Levels,
    [property: JsonPropertyName("events")] Dictionary<string, object> Events,
    [property: JsonPropertyName("skills")] Dictionary<string, object> Skills,
    [property: JsonPropertyName("classes")] Dictionary<string, object> Classes,
    [property: JsonPropertyName("games")] Dictionary<string, object> Games,
    [property: JsonPropertyName("sets")] Dictionary<string, object> Sets,
    [property: JsonPropertyName("drops")] Dictionary<string, object> Drops,
    [property: JsonPropertyName("sprites")] Dictionary<string, object> Sprites
);