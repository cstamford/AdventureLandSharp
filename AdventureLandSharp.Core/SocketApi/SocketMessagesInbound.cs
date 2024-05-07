﻿using AdventureLandSharp.Core.Util;
using System.ComponentModel;
using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AdventureLandSharp.Core.SocketApi;

[AttributeUsage(AttributeTargets.Struct)]
public class InboundSocketMessageAttribute(string name, bool debug = false) : Attribute {
    public string Name => name;
    public bool Debug => debug;
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

    [InboundSocketMessage("hit")]
    public readonly record struct HitData(
        [property: JsonPropertyName("hid")] string OwnerId,
        [property: JsonPropertyName("source")] string Source,
        [property: JsonPropertyName("id")] string TargetId,
        [property: JsonPropertyName("damage")] double Damage,
        [property: JsonPropertyName("kill")] bool Kill
    );

    [InboundSocketMessage("invite")]
    public readonly record struct PartyInviteData(
        [property: JsonPropertyName("name")] string Name
    );

    [InboundSocketMessage("magiport")]
    public readonly record struct MagiportRequestData(
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

    [InboundSocketMessage("request")]
    public readonly record struct PartyRequestData(
        [property: JsonPropertyName("name")] string Name
    );

    [InboundSocketMessage("party_update")]
    public readonly record struct PartyUpdateData(
        [property: JsonConverter(typeof(JsonConverterArrayOrFalse<string>)), JsonPropertyName("list")] string[] Members
    );

    [InboundSocketMessage("server_info")]
    public readonly record struct ServerInfo(
        [property: JsonPropertyName("schedule")] ServerInfo_Schedule Schedule,

        [property: JsonConverter(typeof(JsonConverterBool)), JsonPropertyName("egghunt")] bool IsEaster,
        [property: JsonConverter(typeof(JsonConverterBool)), JsonPropertyName("halloween")] bool IsHalloween,
        [property: JsonConverter(typeof(JsonConverterBool)), JsonPropertyName("holidayseason")] bool IsHolidaySeason,
        [property: JsonConverter(typeof(JsonConverterBool)), JsonPropertyName("lunarnewyear")] bool IsLunarNewYear,
        [property: JsonConverter(typeof(JsonConverterBool)), JsonPropertyName("valentines")] bool IsValentines,

        [property: JsonPropertyName("crabxx")] ServerInfo_EventMonster? BigAssCrab,
        [property: JsonPropertyName("dragold")] ServerInfo_EventMonster? Dragold,
        [property: JsonPropertyName("franky")] ServerInfo_EventMonster? Franky,
        [property: JsonPropertyName("grinch")] ServerInfo_EventMonster? Grinch,
        [property: JsonPropertyName("icegolem")] ServerInfo_EventMonster? IceGolem,
        [property: JsonPropertyName("mrgreen")] ServerInfo_EventMonster? MrGreen,
        [property: JsonPropertyName("mrpumpkin")] ServerInfo_EventMonster? MrPumpkin,
        [property: JsonPropertyName("pinkgoo")] ServerInfo_EventMonster? PinkGoo,
        [property: JsonPropertyName("snowman")] ServerInfo_EventMonster? Snowman,
        [property: JsonPropertyName("tiger")] ServerInfo_EventMonster? Tiger,
        [property: JsonPropertyName("wabbit")] ServerInfo_EventMonster? Wabbit
    );

    public readonly record struct ServerInfo_EventMonster(
        [property: JsonPropertyName("live")] bool IsLive,
        [property: JsonPropertyName("map")] string MapName,
        [property: JsonPropertyName("x")] float MapX,
        [property: JsonPropertyName("y")] float MapY,
        [property: JsonPropertyName("hp")] int Health,
        [property: JsonPropertyName("max_hp")] int MaxHealth
    );

    public readonly record struct ServerInfo_Schedule(
        [property: JsonPropertyName("time_offset")] int TimeOffset,
        [property: JsonPropertyName("dailies")] int[] DailiesTimes,
        [property: JsonPropertyName("nightlies")] int[] NightliesTimes,
        [property: JsonPropertyName("night")] bool IsNight
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

    [InboundSocketMessage("skill_timeout")]
    public readonly record struct SkillTimeoutData(
        [property: JsonPropertyName("name")] string SkillName,
        [property: JsonPropertyName("ms")] int MillisecondsUntilReady /* excluding latency */
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
    [property: JsonPropertyName("y")] float Y)
{
    public Vector2 Position => new(X, Y);
};

public readonly record struct EntityStats(
    [property: JsonPropertyName("armor")] float Armour,
    [property: JsonPropertyName("attack")] float AttackDamage,
    [property: JsonPropertyName("frequency")] float AttackFrequency,
    [property: JsonPropertyName("range")] float AttackRange,
    [property: JsonPropertyName("resistance")] float Resistance,
    [property: JsonPropertyName("speed")] float Speed,
    [property: JsonPropertyName("xp")] float Xp)
{
    public EntityStats(GameDataMonster monsterDef) : this(
        Armour: default,
        AttackDamage: (float)monsterDef.Attack,
        AttackFrequency: (float)monsterDef.Frequency,
        AttackRange: (float)monsterDef.Range,
        Resistance: default,
        Speed: (float)monsterDef.Speed,
        Xp: (float)monsterDef.Xp)
    { }

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
    [property: JsonConverter(typeof(JsonConverterBool)), JsonPropertyName("rip")] bool Dead,
    [property: JsonPropertyName("hp"), JsonRequired()] int Hp,
    [property: JsonPropertyName("mp"), JsonRequired()] int Mp,
    [property: JsonPropertyName("max_hp")] int MaxHp,
    [property: JsonPropertyName("max_mp")] int MaxMp
) {
    public EntityVitals(GameDataMonster monsterDef) : this(
        Dead: false,
        Hp: (int)monsterDef.Hp,
        MaxHp: (int)monsterDef.Hp,
        Mp: (int)monsterDef.Mp,
        MaxMp: (int)monsterDef.Mp)
    { }

    public EntityVitals Update(JsonElement source) => this with {
        Dead = source.GetBool("rip", false),
        Hp = source.GetInt("hp", Hp),
        Mp = source.GetInt("mp", Mp),
        MaxHp = source.GetInt("max_hp", MaxHp),
        MaxMp = source.GetInt("max_mp", MaxMp)
    };
}

public readonly record struct Item(
    [property: JsonPropertyName("name")] string Name, 
    [property: JsonPropertyName("level")] long Level,
    [property: JsonPropertyName("q"), DefaultValue(1)] long Quantity,
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

public readonly record struct PlayerBank(
    [property: JsonPropertyName("gold")] long Gold,
    [property: JsonPropertyName("items0")] Item?[]? Tab0,
    [property: JsonPropertyName("items1")] Item?[]? Tab1,
    [property: JsonPropertyName("items2")] Item?[]? Tab2,
    [property: JsonPropertyName("items3")] Item?[]? Tab3,
    [property: JsonPropertyName("items4")] Item?[]? Tab4,
    [property: JsonPropertyName("items5")] Item?[]? Tab5,
    [property: JsonPropertyName("items6")] Item?[]? Tab6,
    [property: JsonPropertyName("items7")] Item?[]? Tab7,
    [property: JsonPropertyName("items8")] Item?[]? Tab8,
    [property: JsonPropertyName("items9")] Item?[]? Tab9,
    [property: JsonPropertyName("items10")] Item?[]? Tab10,
    [property: JsonPropertyName("items11")] Item?[]? Tab11,
    [property: JsonPropertyName("items12")] Item?[]? Tab12,
    [property: JsonPropertyName("items13")] Item?[]? Tab13,
    [property: JsonPropertyName("items14")] Item?[]? Tab14,
    [property: JsonPropertyName("items15")] Item?[]? Tab15,
    [property: JsonPropertyName("items16")] Item?[]? Tab16,
    [property: JsonPropertyName("items17")] Item?[]? Tab17,
    [property: JsonPropertyName("items18")] Item?[]? Tab18,
    [property: JsonPropertyName("items19")] Item?[]? Tab19,
    [property: JsonPropertyName("items20")] Item?[]? Tab20,
    [property: JsonPropertyName("items21")] Item?[]? Tab21,
    [property: JsonPropertyName("items22")] Item?[]? Tab22,
    [property: JsonPropertyName("items23")] Item?[]? Tab23,
    [property: JsonPropertyName("items24")] Item?[]? Tab24,
    [property: JsonPropertyName("items25")] Item?[]? Tab25,
    [property: JsonPropertyName("items26")] Item?[]? Tab26,
    [property: JsonPropertyName("items27")] Item?[]? Tab27,
    [property: JsonPropertyName("items28")] Item?[]? Tab28,
    [property: JsonPropertyName("items29")] Item?[]? Tab29,
    [property: JsonPropertyName("items30")] Item?[]? Tab30,
    [property: JsonPropertyName("items31")] Item?[]? Tab31,
    [property: JsonPropertyName("items32")] Item?[]? Tab32,
    [property: JsonPropertyName("items33")] Item?[]? Tab33,
    [property: JsonPropertyName("items34")] Item?[]? Tab34,
    [property: JsonPropertyName("items35")] Item?[]? Tab35,
    [property: JsonPropertyName("items36")] Item?[]? Tab36,
    [property: JsonPropertyName("items37")] Item?[]? Tab37,
    [property: JsonPropertyName("items38")] Item?[]? Tab38,
    [property: JsonPropertyName("items39")] Item?[]? Tab39,
    [property: JsonPropertyName("items40")] Item?[]? Tab40,
    [property: JsonPropertyName("items41")] Item?[]? Tab41,
    [property: JsonPropertyName("items42")] Item?[]? Tab42,
    [property: JsonPropertyName("items43")] Item?[]? Tab43,
    [property: JsonPropertyName("items44")] Item?[]? Tab44,
    [property: JsonPropertyName("items45")] Item?[]? Tab45,
    [property: JsonPropertyName("items46")] Item?[]? Tab46,
    [property: JsonPropertyName("items47")] Item?[]? Tab47
) {
    public Item?[]? this[int tab] => GetTab(tab);

    public Item?[]? GetTab(int tab) => tab switch {
        0 => Tab0,   1 => Tab1,   2 => Tab2,   3 => Tab3,   4 => Tab4,   5 => Tab5,
        6 => Tab6,   7 => Tab7,   8 => Tab8,   9 => Tab9,   10 => Tab10, 11 => Tab11,
        12 => Tab12, 13 => Tab13, 14 => Tab14, 15 => Tab15, 16 => Tab16, 17 => Tab17,
        18 => Tab18, 19 => Tab19, 20 => Tab20, 21 => Tab21, 22 => Tab22, 23 => Tab23,
        24 => Tab24, 25 => Tab25, 26 => Tab26, 27 => Tab27, 28 => Tab28, 29 => Tab29,
        30 => Tab30, 31 => Tab31, 32 => Tab32, 33 => Tab33, 34 => Tab34, 35 => Tab35,
        36 => Tab36, 37 => Tab37, 38 => Tab38, 39 => Tab39, 40 => Tab40, 41 => Tab41,
        42 => Tab42, 43 => Tab43, 44 => Tab44, 45 => Tab45, 46 => Tab46, 47 => Tab47,
        _ => throw new ArgumentOutOfRangeException(nameof(tab))
    };

    public IEnumerable<Item?[]?> AllTabs => Enumerable.Range(0, 48).Select(GetTab);
    public IEnumerable<(int Index, Item?[] Tab)> ValidTabs => AllTabs
        .Select((tab, i) => (i, tab))
        .Where(x => x.tab != null)
        .Select(x => (x.i, x.tab!)
    );

    public int SlotsFree => ValidTabs.Sum(tab => GetFreeSlotsInTab(tab.Tab));
    public int SlotsUsed => 42 * ValidTabs.Count() - SlotsFree;

    public static string GetMapNameForTabIdx(int tab) {
        if (tab <= 7) {
            return "bank";
        } else if (tab <= 23) {
            return "bank_b";
        } else if (tab <= 47) {
            return "bank_u";
        }

        throw new ArgumentOutOfRangeException(nameof(tab));
    }

    public static int GetFreeSlotsInTab(Item?[] tab) {
        return 42 - tab.Count(x => x != null);
    }
}

public readonly record struct PlayerInventory(
    [property: JsonPropertyName("gold")] long Gold,
    [property: JsonPropertyName("items")] List<Item?> Items)
{
    public int SlotsFree => Items.Count(x => x == null);
    public int SlotsUsed => 42 - SlotsFree;

    public PlayerInventory Update(JsonElement source) => this with {
        Gold = source.GetLong("gold", Gold),
        Items = source.TryGetProperty("items", out JsonElement items) ? items.Deserialize<List<Item?>>()! : Items
    };

    public readonly int FindSlotId(string name) => Items.FindIndex(item =>
        item?.Name == name);

    public readonly int FindSlotId(string name, Func<Item, bool> pred) => Items.FindIndex(item => 
        item != null &&
        item.Value.Name == name &&
        pred(item.Value));
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
