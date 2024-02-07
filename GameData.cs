using System.Text.Json;
using System.Text.Json.Serialization;

namespace AdventureLandSharp;

public record struct GameLevelGeometry(
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

public record struct GameData(
    [property: JsonPropertyName("positions")] Dictionary<string, object> Positions,
    [property: JsonPropertyName("titles")] Dictionary<string, object> Titles,
    [property: JsonPropertyName("tilesets")] Dictionary<string, object> Tilesets,
    [property: JsonPropertyName("images")] Dictionary<string, object> Images,
    [property: JsonPropertyName("imagesets")] Dictionary<string, object> Imagesets,
    [property: JsonPropertyName("dimensions")] Dictionary<string, object> Dimensions,
    [property: JsonPropertyName("emotions")] Dictionary<string, object> Emotions,
    [property: JsonPropertyName("multipliers")] Dictionary<string, object> Multipliers,
    [property: JsonPropertyName("maps")] Dictionary<string, object> Maps,
    [property: JsonPropertyName("version")] int Version,
    [property: JsonPropertyName("cosmetics")] Dictionary<string, object> Cosmetics,
    [property: JsonPropertyName("conditions")] Dictionary<string, object> Conditions,
    [property: JsonPropertyName("monsters")] Dictionary<string, object> Monsters,
    [property: JsonPropertyName("achievements")] Dictionary<string, object> Achievements,
    [property: JsonPropertyName("docs")] Dictionary<string, object> Docs,
    [property: JsonPropertyName("dismantle")] Dictionary<string, object> Dismantle,
    [property: JsonPropertyName("projectiles")] Dictionary<string, object> Projectiles,
    [property: JsonPropertyName("tokens")] Dictionary<string, object> Tokens,
    [property: JsonPropertyName("craft")] Dictionary<string, object> Craft,
    [property: JsonPropertyName("animations")] Dictionary<string, object> Animations,
    [property: JsonPropertyName("npcs")] Dictionary<string, object> Npcs,
    [property: JsonPropertyName("geometry")] Dictionary<string, GameLevelGeometry> Geometry,
    [property: JsonPropertyName("items")] Dictionary<string, object> Items,
    [property: JsonPropertyName("levels")] Dictionary<string, object> Levels,
    [property: JsonPropertyName("events")] Dictionary<string, object> Events,
    [property: JsonPropertyName("skills")] Dictionary<string, object> Skills,
    [property: JsonPropertyName("classes")] Dictionary<string, object> Classes,
    [property: JsonPropertyName("games")] Dictionary<string, object> Games,
    [property: JsonPropertyName("sets")] Dictionary<string, object> Sets,
    [property: JsonPropertyName("drops")] Dictionary<string, object> Drops,
    [property: JsonPropertyName("sprites")] Dictionary<string, object> Sprites
);
