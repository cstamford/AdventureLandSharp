using System.Text.Json.Serialization;

namespace AdventureLandSharp.Core.SocketApi;

[AttributeUsage(AttributeTargets.Struct)]
public class OutboundSocketMessageAttribute(string name) : Attribute {
    public string Name => name;
}

public static class Outbound {
    [OutboundSocketMessage("attack")]
    public readonly record struct Attack(
        [property: JsonPropertyName("id")] string Id
    );

    [OutboundSocketMessage("auth")]
    public readonly record struct Auth(
        [property: JsonPropertyName("auth")] string AuthToken,
        [property: JsonPropertyName("character")] string CharacterId,
        [property: JsonPropertyName("user")] string UserId,
        [property: JsonPropertyName("width")] long Width,
        [property: JsonPropertyName("height")] long Height,
        [property: JsonPropertyName("scale")] long Scale,
        [property: JsonPropertyName("no_html")] bool NoHtml,
        [property: JsonPropertyName("no_graphics")] bool NoGraphics
    );

    [OutboundSocketMessage("buy")]
    public readonly record struct Buy(
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("quantity")] long Quantity
    );

    [OutboundSocketMessage("equip")]
    public readonly record struct Equip(
        [property: JsonPropertyName("num")] long Slot
    );

    [OutboundSocketMessage("equip")]
    public readonly record struct EquipSlot(
        [property: JsonPropertyName("num")] long Slot,
        [property: JsonPropertyName("slot")] string SlotName
    );

    [OutboundSocketMessage("heal")]
    public readonly record struct Heal(
        [property: JsonPropertyName("id")] string Id
    );

    [OutboundSocketMessage("loaded")]
    public readonly record struct Loaded(
        [property: JsonPropertyName("success")] bool Success,
        [property: JsonPropertyName("width")] long Width,
        [property: JsonPropertyName("height")] long Height,
        [property: JsonPropertyName("scale")] long Scale
    );

    [OutboundSocketMessage("leave")]
    public readonly record struct Leave;

    [OutboundSocketMessage("move")]
    public readonly record struct Move(
        [property: JsonPropertyName("x")] double X,
        [property: JsonPropertyName("y")] double Y,
        [property: JsonPropertyName("going_x")] double TargetX,
        [property: JsonPropertyName("going_y")] double TargetY,
        [property: JsonPropertyName("m")] long MapId
    );

    [OutboundSocketMessage("loot")]
    public readonly record struct OpenChest(
        [property: JsonPropertyName("id")] string Id
    );

    [OutboundSocketMessage("party")]
    public readonly record struct PartyAccept([property: JsonPropertyName("name")] string Name) {
        [property: JsonPropertyName("event")] public string Event { get; } = "raccept";
    }

    [OutboundSocketMessage("party")]
    public readonly record struct PartyInvite([property: JsonPropertyName("name")] string Name) {
        [property: JsonPropertyName("event")] public string Event { get; } = "request";
    }

    [OutboundSocketMessage("respawn")]
    public readonly record struct Respawn;

    [OutboundSocketMessage("send")]
    public readonly record struct SendGold(
        [property: JsonPropertyName("name")] string To,
        [property: JsonPropertyName("gold")] double Amount
    );

    [OutboundSocketMessage("send")]
    public readonly record struct SendItem(
        [property: JsonPropertyName("name")] string To,
        [property: JsonPropertyName("num")] long SlotId,
        [property: JsonPropertyName("q")] long Quantity
    );

    [OutboundSocketMessage("sell")]
    public readonly record struct SellItem(
        [property: JsonPropertyName("num")] long SlotId,
        [property: JsonPropertyName("q")] long Quantity
    );

    [OutboundSocketMessage("town")]
    public readonly record struct Town;

    [OutboundSocketMessage("transport")]
    public readonly record struct Transport(
        [property: JsonPropertyName("to")] string Map,
        [property: JsonPropertyName("s")] long SpawnId
    );

    [OutboundSocketMessage("use")]
    public readonly record struct UseItem(
        [property: JsonPropertyName("item")] string Item
    );

    [OutboundSocketMessage("skill")]
    public readonly record struct UseSkill(
        [property: JsonPropertyName("name")] string Name
    );

    [OutboundSocketMessage("skill")]
    public readonly record struct UseSkillOnId(
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("id")] string Id
    );

    [OutboundSocketMessage("skill")]
    public readonly record struct UseSkillOnIds(
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("ids")] string[] Ids
    );
}
