using System.Text.Json;
using System.Text.Json.Serialization;

namespace AdventureLandSharp.Core.SocketApi;

[AttributeUsage(AttributeTargets.Struct)]
public class InboundSocketMessageAttribute(string name) : Attribute {
    public string Name => name;
}

public static class Inbound {
    [InboundSocketMessage("action")]
    public readonly record struct ActionData(
        [property: JsonPropertyName("anim")] string AnimationType,
        [property: JsonPropertyName("attacker")] string Attacker,
        [property: JsonPropertyName("damage")] double? Damage,
        [property: JsonPropertyName("heal")] double? Heal,
        [property: JsonPropertyName("m")] int MapIndex,
        [property: JsonPropertyName("no_lines")] bool? NoLines,
        [property: JsonPropertyName("projectile")] string Projectile,
        [property: JsonPropertyName("pid")] string ProjectileId,
        [property: JsonPropertyName("target")] string Target,
        [property: JsonPropertyName("x")] double X,
        [property: JsonPropertyName("y")] double Y
    );

    [InboundSocketMessage("chat_log")]
    public readonly record struct ChatMessageData(
        [property: JsonPropertyName("color")] string Colour,
        [property: JsonPropertyName("id")] string Id,
        [property: JsonPropertyName("message")] string Message,
        [property: JsonPropertyName("owner")] string Owner
    );

    [InboundSocketMessage("chest_opened")]
    public readonly record struct ChestOpenedData(
        [property: JsonPropertyName("gone")] bool Gone,
        [property: JsonPropertyName("id")] string Id,
        [property: JsonPropertyName("opener")] string Opener
    );

    [InboundSocketMessage("correction")]
    public readonly record struct CorrectionData(
        [property: JsonPropertyName("x")] double X,
        [property: JsonPropertyName("y")] double Y
    );

    [InboundSocketMessage("death")]
    public readonly record struct DeathData(
        [property: JsonPropertyName("id")] string? Id
    );

    [InboundSocketMessage("disappear")]
    public readonly record struct DisappearData(
        [property: JsonPropertyName("id")] string? Id
    );

    [InboundSocketMessage("drop")]
    public readonly record struct ChestDropData(
        [property: JsonPropertyName("chest")] string ChestType,
        [property: JsonPropertyName("id")] string Id,
        [property: JsonPropertyName("items")] string[] Items,
        [property: JsonPropertyName("map")] string Map,
        [property: JsonPropertyName("x")] float X,
        [property: JsonPropertyName("y")] float Y
    );

    [InboundSocketMessage("entities")]
    public readonly record struct EntitiesData(
        [property: JsonPropertyName("in")] string In,
        [property: JsonPropertyName("monsters")] List<Dictionary<string, JsonElement>> Monsters,
        [property: JsonPropertyName("players")] List<Dictionary<string, JsonElement>> Players,
        [property: JsonPropertyName("type")] string Type
    );

    [InboundSocketMessage("game_event")]
    public readonly record struct GameEventData(
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("map")] string Map,
        [property: JsonPropertyName("A")] int? A,
        [property: JsonPropertyName("B")] int? B
    );

    [InboundSocketMessage("game_response")]
    public readonly record struct GameResponseData(
        [property: JsonPropertyName("response")] string Response,
        [property: JsonPropertyName("place")] string? Place,
        [property: JsonPropertyName("failed")] bool? Failed,
        [property: JsonPropertyName("reason")] string? Reason,
        [property: JsonPropertyName("cevent")] object? CharacterEvent,
        [property: JsonPropertyName("event")] object? GeneralEvent,
        [property: JsonPropertyName("home")] string? Home,
        [property: JsonPropertyName("hours")] float? Hours,
        [property: JsonPropertyName("upgrade")] bool? Upgrade,
        [property: JsonPropertyName("level")] int? Level,
        [property: JsonPropertyName("num")] int? Number,
        [property: JsonPropertyName("stale")] bool? Stale,
        [property: JsonPropertyName("gold")] double? Gold,
        [property: JsonPropertyName("item")] string? Item,
        [property: JsonPropertyName("skin")] string? Skin,
        [property: JsonPropertyName("message")] string? Message,
        [property: JsonPropertyName("id")] string? Id,
        [property: JsonPropertyName("autoclose")] bool? Autoclose,
        [property: JsonPropertyName("to")] string? To,
        [property: JsonPropertyName("chance")] float? Chance,
        [property: JsonPropertyName("challenger")] string? Challenger,
        [property: JsonPropertyName("vs")] string? Vs,
        [property: JsonPropertyName("scroll")] string? Scroll,
        [property: JsonPropertyName("q")] int? Quantity,
        [property: JsonPropertyName("h")] int? Have,
        [property: JsonPropertyName("cx")] string? Cosmetic,
        [property: JsonPropertyName("emx")] string? Emote,
        [property: JsonPropertyName("acx")] string? ActiveCosmetic,
        [property: JsonPropertyName("need")] string? Need,
        [property: JsonPropertyName("conditions")] string? Conditions,
        [property: JsonPropertyName("from")] string? From,
        [property: JsonPropertyName("cost")] double? Cost,
        [property: JsonPropertyName("offering")] bool? Offering,
        [property: JsonPropertyName("check")] bool? Check,
        [property: JsonPropertyName("flip")] bool? IsFlip
    );

    [InboundSocketMessage("hit")]
    public readonly record struct HitData(
        [property: JsonPropertyName("hid")] string OwnerId,
        [property: JsonPropertyName("id")] string TargetId,
        [property: JsonPropertyName("pid")] string ProjectileId,
        [property: JsonPropertyName("damage")] double? Damage,
        [property: JsonPropertyName("heal")] double? Heal,
        [property: JsonPropertyName("miss")] bool Miss,
        [property: JsonPropertyName("evade")] bool Evade,
        [property: JsonPropertyName("avoid")] bool Avoid,
        [property: JsonPropertyName("x")] float? LocationX,
        [property: JsonPropertyName("y")] float? LocationY,
        [property: JsonPropertyName("crit")] float? Crit,
        [property: JsonPropertyName("map")] string Map,
        [property: JsonPropertyName("in")] string InId,
        [property: JsonPropertyName("stacked")] List<string>? StackedIds,
        [property: JsonPropertyName("mobbing")] double? Mobbing,
        [property: JsonPropertyName("goldsteal")] double? GoldSteal,
        [property: JsonPropertyName("reflect")] bool Reflect,
        [property: JsonPropertyName("projectile")] string? ProjectileType,
        [property: JsonPropertyName("dreturn")] double? DamageReturn,
        [property: JsonPropertyName("source")] string HealSource
    );

    [InboundSocketMessage("invite")]
    public readonly record struct PartyInviteData(
        [property: JsonPropertyName("name")] string Name
    );

    [InboundSocketMessage("magiport")]
    public readonly record struct MagiportData(
        [property: JsonPropertyName("name")] string Name
    );

    [InboundSocketMessage("new_map")]
    public readonly record struct NewMapData(
        [property: JsonPropertyName("direction")] double? Direction,
        [property: JsonPropertyName("entities")] EntitiesData Entities,
        [property: JsonPropertyName("m")] long MapId,
        [property: JsonPropertyName("name")] string MapName,
        [property: JsonPropertyName("x")] double PlayerX,
        [property: JsonPropertyName("y")] double PlayerY
    );

    [InboundSocketMessage("party_request")]
    public readonly record struct PartyRequestData(
        [property: JsonPropertyName("name")] string Name
    );

    [InboundSocketMessage("party_update")]
    public readonly record struct PartyUpdateData(
        [property: JsonPropertyName("list")] List<string>? Members
    );

    [InboundSocketMessage("server_message")]
    public readonly record struct ServerMessageData(
        [property: JsonPropertyName("color")] string Colour,
        [property: JsonPropertyName("item")] string Item,
        [property: JsonPropertyName("log")] bool Log,
        [property: JsonPropertyName("message")] string Message,
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("type")] string Type
    );

    [InboundSocketMessage("upgrade")]
    public readonly record struct UpgradeData(
        [property: JsonPropertyName("type")] string Type,
        [property: JsonPropertyName("success")] bool Success
    );

    [InboundSocketMessage("welcome")]
    public readonly record struct WelcomeData(
        [property: JsonPropertyName("character")] string Character,
        [property: JsonPropertyName("gameplay")] string Gameplay,
        [property: JsonPropertyName("in")] string In,
        [property: JsonPropertyName("pvp")] bool IsPvp,
        [property: JsonPropertyName("map")] string Map,
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("region")] string Region,
        [property: JsonPropertyName("S")] string S,
        [property: JsonPropertyName("x")] double X,
        [property: JsonPropertyName("y")] double Y
    );
}
