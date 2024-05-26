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

    [OutboundSocketMessage("bank")]
    public readonly record struct BankDeposit(
        [property: JsonPropertyName("inv")] long InventoryIdx,
        [property: JsonPropertyName("pack")] string TabPackIdx,
        [property: JsonPropertyName("str")] long TabItemIdx
    ) {
        [property: JsonPropertyName("operation")] public string Operation { get; } = "swap";

        public BankDeposit(long inventorySlot, long storageSlot) : this(
            InventoryIdx: inventorySlot,
            TabPackIdx: $"items{storageSlot}",
            TabItemIdx: -1) 
        { }
    }

    [OutboundSocketMessage("blend")]
    public readonly record struct Blend;

    [OutboundSocketMessage("buy")]
    public readonly record struct Buy(
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("quantity")] long Quantity
    );

    [OutboundSocketMessage("equip")]
    public readonly record struct Equip(
        [property: JsonPropertyName("num")] long InvIdx
    );

    [OutboundSocketMessage("equip")]
    public readonly record struct EquipSlot(
        [property: JsonPropertyName("slot")] string SlotName,
        [property: JsonPropertyName("num")] long InvIdx
    );

    [OutboundSocketMessage("heal")]
    public readonly record struct Heal(
        [property: JsonPropertyName("id")] string Id
    );

    [OutboundSocketMessage("join")]
    public readonly record struct Join(
        [property: JsonPropertyName("name")] string Name
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

    [OutboundSocketMessage("magiport")]
    public readonly record struct MagiportResponseData(
        [property: JsonPropertyName("name")] string Name
    );

    [OutboundSocketMessage("move")]
    public readonly record struct Move(
        [property: JsonPropertyName("x")] double X,
        [property: JsonPropertyName("y")] double Y,
        [property: JsonPropertyName("going_x")] double TargetX,
        [property: JsonPropertyName("going_y")] double TargetY,
        [property: JsonPropertyName("m")] long MapId
    );

    [OutboundSocketMessage("open_chest")]
    public readonly record struct OpenChest(
        [property: JsonPropertyName("id")] string Id
    );

    [OutboundSocketMessage("party")]
    public readonly record struct PartyAccept(
        [property: JsonPropertyName("name")] string Name)
    {
        [property: JsonPropertyName("event")] public string Event { get; } = "raccept";
    }

    [OutboundSocketMessage("party")]
    public readonly record struct PartyInvite(
        [property: JsonPropertyName("name")] string Name)
    {
        [property: JsonPropertyName("event")] public string Event { get; } = "request";
    }

    [OutboundSocketMessage("ping_trig")]
    public readonly record struct Ping(
        [property: JsonPropertyName("id")] long Id
    );

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

    [OutboundSocketMessage("send_updates")]
    public readonly record struct SendUpdates;

    [OutboundSocketMessage("stop")]
    public readonly record struct Stop(
        [property: JsonPropertyName("action")] string? Action
    );

    [OutboundSocketMessage("throw")]
    public readonly record struct Throw(
        [property: JsonPropertyName("num")] int InventorySlot
    );

    [OutboundSocketMessage("town")]
    public readonly record struct Town;

    [OutboundSocketMessage("tracker")]
    public readonly record struct Tracker;

    [OutboundSocketMessage("transport")]
    public readonly record struct Transport(
        [property: JsonPropertyName("to")] string Map,
        [property: JsonPropertyName("s")] long SpawnId
    );

    [OutboundSocketMessage("use")]
    public readonly record struct Use(
        [property: JsonPropertyName("item")] string Item
    );

    [OutboundSocketMessage("unequip")]
    public readonly record struct UnequipSlot(
        [property: JsonPropertyName("slot")] string SlotName
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

    [OutboundSocketMessage("skill")]
    public readonly record struct UseSkillOnPosition(
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("x")] double X,
        [property: JsonPropertyName("y")] double Y
    );

    [OutboundSocketMessage("skill")]
    public readonly record struct UseSkillEnergize(
        [property: JsonPropertyName("id")] string Id,
        [property: JsonPropertyName("mp")] int Mp)
    {
        [property: JsonPropertyName("name")] public string Name { get; } = "energize";
    }
}
