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
        [property: JsonPropertyName("x")] float X,
        [property: JsonPropertyName("y")] float Y
    );

    [InboundSocketMessage("death")]
    public readonly record struct DeathData(
        [property: JsonPropertyName("id")] string Id
    );

    [InboundSocketMessage("disappear")]
    public readonly record struct DisappearData(
        [property: JsonPropertyName("id")] string Id
    );

    [InboundSocketMessage("drop")]
    public readonly record struct ChestDropData(
        [property: JsonPropertyName("chest")] string ChestType,
        [property: JsonPropertyName("id")] string Id,
        [property: JsonPropertyName("map")] string Map,
        [property: JsonPropertyName("owners")] string[] Owners,
        [property: JsonPropertyName("x")] float X,
        [property: JsonPropertyName("y")] float Y
    );

    [InboundSocketMessage("entities")]
    public readonly record struct EntitiesData(
        [property: JsonPropertyName("in")] string In,
        [property: JsonPropertyName("monsters")] List<JsonElement> Monsters,
        [property: JsonPropertyName("players")] List<JsonElement> Players,
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
        [property: JsonPropertyName("x")] float PlayerX,
        [property: JsonPropertyName("y")] float PlayerY
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
        [property: JsonPropertyName("x")] float X,
        [property: JsonPropertyName("y")] float Y
    );
}

public readonly record struct DropData(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("x")] float X,
    [property: JsonPropertyName("y")] float Y) {
    public Vector2 Position => new(X, Y);
}

public readonly record struct EntityStats(
    [property: JsonPropertyName("armor")] float Armour,
    [property: JsonPropertyName("attack")] float AttackDamage,
    [property: JsonPropertyName("frequency")] float AttackFrequency,
    [property: JsonPropertyName("range")] float AttackRange,
    [property: JsonPropertyName("resistance")] float Resistance,
    [property: JsonPropertyName("speed")] float Speed,
    [property: JsonPropertyName("xp")] float Xp) {
    public EntityStats(GameDataMonster monsterDef) : this(
        default,
        (float)monsterDef.Attack,
        (float)monsterDef.Frequency,
        (float)monsterDef.Range,
        default,
        (float)monsterDef.Speed,
        (float)monsterDef.Xp) { }

    public EntityStats Update(JsonElement source) => this with {
        Armour = source.GetFloat("armor", Armour),
        AttackDamage = source.GetFloat("attack", AttackDamage),
        AttackFrequency = source.GetFloat("frequency", AttackFrequency),
        AttackRange = source.GetFloat("range", AttackRange),
        Resistance = source.GetFloat("resistance", Resistance),
        Speed = source.GetFloat("speed", Speed),
        Xp = source.GetFloat("xp", Xp)
    };
}

public readonly record struct EntityVitals(
    [property: JsonConverter(typeof(JsonConverterBool))] [property: JsonPropertyName("rip")] bool Dead,
    [property: JsonPropertyName("hp")] [property: JsonRequired] float Hp,
    [property: JsonPropertyName("mp")] [property: JsonRequired] float Mp,
    [property: JsonPropertyName("max_hp")] float MaxHp,
    [property: JsonPropertyName("max_mp")] float MaxMp
) {
    public EntityVitals(GameDataMonster monsterDef) : this(
        false,
        (float)monsterDef.Hp,
        MaxHp: (float)monsterDef.Hp,
        Mp: (float)monsterDef.Mp,
        MaxMp: (float)monsterDef.Mp) { }

    public EntityVitals Update(JsonElement source) => this with {
        Dead = source.GetBool("rip", false),
        Hp = source.GetFloat("hp", Hp),
        Mp = source.GetFloat("mp", Mp),
        MaxHp = source.GetFloat("max_hp", MaxHp),
        MaxMp = source.GetFloat("max_mp", MaxMp)
    };
}

public readonly record struct Item(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("level")] long Level,
    [property: JsonPropertyName("q")] [property: DefaultValue(1)] long Quantity,
    [property: JsonPropertyName("p")] string? SpecialType
);

public readonly record struct PlayerEquipment(
    [property: JsonPropertyName("ring1")] Item? Ring1,
    [property: JsonPropertyName("ring2")] Item? Ring2,
    [property: JsonPropertyName("earring1")] Item? Earring1,
    [property: JsonPropertyName("earring2")] Item? Earring2,
    [property: JsonPropertyName("belt")] Item? Belt,
    [property: JsonPropertyName("mainhand")] Item? MainHand,
    [property: JsonPropertyName("offhand")] Item? OffHand,
    [property: JsonPropertyName("helmet")] Item? Helmet,
    [property: JsonPropertyName("chest")] Item? Chest,
    [property: JsonPropertyName("pants")] Item? Pants,
    [property: JsonPropertyName("shoes")] Item? Shoes,
    [property: JsonPropertyName("gloves")] Item? Gloves,
    [property: JsonPropertyName("amulet")] Item? Amulet,
    [property: JsonPropertyName("orb")] Item? Orb,
    [property: JsonPropertyName("elixir")] Item? Elixir,
    [property: JsonPropertyName("cape")] Item? Cape
);

public readonly record struct PlayerInventory(
    [property: JsonPropertyName("gold")] long Gold,
    [property: JsonPropertyName("items")] List<Item?> Items) {
    public PlayerInventory Update(JsonElement source) => this with {
        Gold = source.GetLong("gold", Gold),
        Items = source.TryGetProperty("items", out JsonElement items) ? items.Deserialize<List<Item?>>()! : Items
    };

    public readonly int FindSlotId(string name) {
        return Items.FindIndex(item => item?.Name == name);
    }
}

public readonly record struct StatusEffect(
    [property: JsonPropertyName("f")] string Owner,
    [property: JsonPropertyName("ms")] float Duration
);

public readonly record struct StatusEffects(
    [property: JsonPropertyName("authfail")] StatusEffect? AuthFail,
    [property: JsonPropertyName("blink")] StatusEffect? Blink,
    [property: JsonPropertyName("block")] StatusEffect? Block,
    [property: JsonPropertyName("burned")] StatusEffect? Burned,
    [property: JsonPropertyName("charging")] StatusEffect? Charging,
    [property: JsonPropertyName("charmed")] StatusEffect? Charmed,
    [property: JsonPropertyName("cursed")] StatusEffect? Cursed,
    [property: JsonPropertyName("dash")] StatusEffect? Dash,
    [property: JsonPropertyName("dampened")] StatusEffect? Dampened,
    [property: JsonPropertyName("darkblessing")] StatusEffect? DarkBlessing,
    [property: JsonPropertyName("deepfreezed")] StatusEffect? DeepFreezed,
    [property: JsonPropertyName("eburn")] StatusEffect? EBurn,
    [property: JsonPropertyName("eheal")] StatusEffect? EHeal,
    [property: JsonPropertyName("energized")] StatusEffect? Energized,
    [property: JsonPropertyName("easterluck")] StatusEffect? EasterLuck,
    [property: JsonPropertyName("fingered")] StatusEffect? Fingered,
    [property: JsonPropertyName("fishing")] StatusEffect? Fishing,
    [property: JsonPropertyName("frozen")] StatusEffect? Frozen,
    [property: JsonPropertyName("fullguard")] StatusEffect? FullGuard,
    [property: JsonPropertyName("fullguardx")] StatusEffect? FullGuardX,
    [property: JsonPropertyName("halloween0")] StatusEffect? Halloween0,
    [property: JsonPropertyName("halloween1")] StatusEffect? Halloween1,
    [property: JsonPropertyName("halloween2")] StatusEffect? Halloween2,
    [property: JsonPropertyName("hardshell")] StatusEffect? HardShell,
    [property: JsonPropertyName("holidayspirit")] StatusEffect? HolidaySpirit,
    [property: JsonPropertyName("hopsickness")] StatusEffect? HopSickness,
    [property: JsonPropertyName("invis")] StatusEffect? Invis,
    [property: JsonPropertyName("invincible")] StatusEffect? Invincible,
    [property: JsonPropertyName("licenced")] StatusEffect? Licenced,
    [property: JsonPropertyName("marked")] StatusEffect? Marked,
    [property: JsonPropertyName("massproduction")] StatusEffect? MassProduction,
    [property: JsonPropertyName("massproductionpp")] StatusEffect? MassProductionPP,
    [property: JsonPropertyName("mcourage")] StatusEffect? MCourage,
    [property: JsonPropertyName("mfrenzy")] StatusEffect? MFrenzy,
    [property: JsonPropertyName("mining")] StatusEffect? Mining,
    [property: JsonPropertyName("mlifesteal")] StatusEffect? MLifesteal,
    [property: JsonPropertyName("mluck")] StatusEffect? MLuck,
    [property: JsonPropertyName("monsterhunt")] StatusEffect? MonsterHunt,
    [property: JsonPropertyName("mshield")] StatusEffect? MShield,
    [property: JsonPropertyName("newcomersblessing")] StatusEffect? NewcomersBlessing,
    [property: JsonPropertyName("notverified")] StatusEffect? NotVerified,
    [property: JsonPropertyName("penalty_cd")] StatusEffect? PenaltyCD,
    [property: JsonPropertyName("phasedout")] StatusEffect? PhasedOut,
    [property: JsonPropertyName("pickpocket")] StatusEffect? Pickpocket,
    [property: JsonPropertyName("poisoned")] StatusEffect? Poisoned,
    [property: JsonPropertyName("poisonous")] StatusEffect? Poisonous,
    [property: JsonPropertyName("power")] StatusEffect? Power,
    [property: JsonPropertyName("purifier")] StatusEffect? Purifier,
    [property: JsonPropertyName("reflection")] StatusEffect? Reflection,
    [property: JsonPropertyName("rspeed")] StatusEffect? RSpeed,
    [property: JsonPropertyName("sanguine")] StatusEffect? Sanguine,
    [property: JsonPropertyName("shocked")] StatusEffect? Shocked,
    [property: JsonPropertyName("sleeping")] StatusEffect? Sleeping,
    [property: JsonPropertyName("slowness")] StatusEffect? Slowness,
    [property: JsonPropertyName("stack")] StatusEffect? Stack,
    [property: JsonPropertyName("stoned")] StatusEffect? Stoned,
    [property: JsonPropertyName("stunned")] StatusEffect? Stunned,
    [property: JsonPropertyName("sugarrush")] StatusEffect? SugarRush,
    [property: JsonPropertyName("town")] StatusEffect? Town,
    [property: JsonPropertyName("tangled")] StatusEffect? Tangled,
    [property: JsonPropertyName("withdrawal")] StatusEffect? Withdrawal,
    [property: JsonPropertyName("woven")] StatusEffect? Woven,
    [property: JsonPropertyName("warcry")] StatusEffect? WarCry,
    [property: JsonPropertyName("weakness")] StatusEffect? Weakness,
    [property: JsonPropertyName("xpower")] StatusEffect? XPower,
    [property: JsonPropertyName("xshotted")] StatusEffect? XShotted
);
