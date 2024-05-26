using AdventureLandSharp.Core.Util;
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
        [property: JsonPropertyName("crit")] double CritMultiplier,
        [property: JsonPropertyName("lifesteal")] double LifestealHp,
        [property: JsonPropertyName("kill")] bool Kill,
        [property: JsonPropertyName("miss")] bool Miss
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

    [InboundSocketMessage("ping_ack")]
    public readonly record struct PingAckData(
        [property: JsonPropertyName("id")] long Id
    );

    [InboundSocketMessage("server_info")]
    public readonly record struct ServerInfo(
        [property: JsonPropertyName("schedule")] ServerInfo_Schedule Schedule,

        [property: JsonConverter(typeof(JsonConverterBool)), JsonPropertyName("egghunt")] bool IsEaster,
        [property: JsonConverter(typeof(JsonConverterBool)), JsonPropertyName("halloween")] bool IsHalloween,
        [property: JsonConverter(typeof(JsonConverterBool)), JsonPropertyName("holidayseason")] bool IsHolidaySeason,
        [property: JsonConverter(typeof(JsonConverterBool)), JsonPropertyName("lunarnewyear")] bool IsLunarNewYear,
        [property: JsonConverter(typeof(JsonConverterBool)), JsonPropertyName("valentines")] bool IsValentines,

        [property: JsonPropertyName(GameConstants.ABTestingJoinName)] ServerInfo_Event? ABTesting,
        [property: JsonPropertyName(GameConstants.GooBrawlJoinName)] ServerInfo_Event? GooBrawl,

        [property: JsonPropertyName(GameConstants.BigAssCrabMobName)] ServerInfo_EventMonster? BigAssCrab,
        [property: JsonPropertyName(GameConstants.DragoldMobName)] ServerInfo_EventMonster? Dragold,
        [property: JsonPropertyName(GameConstants.FrankyMobName)] ServerInfo_EventMonster? Franky,
        [property: JsonPropertyName(GameConstants.GrinchMobName)] ServerInfo_EventMonster? Grinch,
        [property: JsonPropertyName(GameConstants.IceGolemMobName)] ServerInfo_EventMonster? IceGolem,
        [property: JsonPropertyName(GameConstants.MrGreenMobName)] ServerInfo_EventMonster? MrGreen,
        [property: JsonPropertyName(GameConstants.MrPumpkinMobName)] ServerInfo_EventMonster? MrPumpkin,
        [property: JsonPropertyName(GameConstants.PinkGooMobName)] ServerInfo_EventMonster? PinkGoo,
        [property: JsonPropertyName(GameConstants.SnowmanMobName)] ServerInfo_EventMonster? Snowman,
        [property: JsonPropertyName(GameConstants.TigerMobName)] ServerInfo_EventMonster? Tiger,
        [property: JsonPropertyName(GameConstants.WabbitMobName)] ServerInfo_EventMonster? Wabbit)
    {
        public readonly IEnumerable<(ServerInfo_EventMonsterType Type, ServerInfo_EventMonster? Data)> EventMonsters => [
            (ServerInfo_EventMonsterType.BigAssCrab, BigAssCrab),
            (ServerInfo_EventMonsterType.Dragold, Dragold),
            (ServerInfo_EventMonsterType.Franky, Franky),
            (ServerInfo_EventMonsterType.Grinch, Grinch),
            (ServerInfo_EventMonsterType.IceGolem, IceGolem),
            (ServerInfo_EventMonsterType.MrGreen, MrGreen),
            (ServerInfo_EventMonsterType.MrPumpkin, MrPumpkin),
            (ServerInfo_EventMonsterType.PinkGoo, PinkGoo),
            (ServerInfo_EventMonsterType.Snowman, Snowman),
            (ServerInfo_EventMonsterType.Tiger, Tiger),
            (ServerInfo_EventMonsterType.Wabbit, Wabbit)
        ];
    }

    public enum ServerInfo_EventMonsterType {
        BigAssCrab,
        Dragold,
        Franky,
        Grinch,
        IceGolem,
        MrGreen,
        MrPumpkin,
        PinkGoo,
        Snowman,
        Tiger,
        Wabbit
    }

    public readonly record struct ServerInfo_Event(
        [property: JsonPropertyName("end")] string EndTime
    );

    public readonly record struct ServerInfo_EventMonster(
        [property: JsonPropertyName("live")] bool IsLive,
        [property: JsonPropertyName("map")] string MapName,
        [property: JsonPropertyName("x")] float MapX,
        [property: JsonPropertyName("y")] float MapY,
        [property: JsonPropertyName("hp")] int Health,
        [property: JsonPropertyName("max_hp")] int MaxHealth,
        [property: JsonPropertyName("end")] string EndTime
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

    [InboundSocketMessage("tracker")]
    public readonly record struct Tracker(
        [property: JsonPropertyName("monsters")] JsonElement Monsters,
        [property: JsonPropertyName("monsters_diff")] JsonElement MonstersDiff,
        [property: JsonPropertyName("exchanges")] JsonElement Exchanges,
        [property: JsonPropertyName("maps")] JsonElement Maps,
        [property: JsonPropertyName("tables")] JsonElement Tables,
        [property: JsonPropertyName("max")] TrackerMax Max
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
    [property: JsonPropertyName("xp")] float? Xp)
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
        Xp = source.TryGetProperty("xp", out JsonElement xp) && xp.ValueKind == JsonValueKind.Number ? xp.GetSingle() : Xp
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

    [JsonIgnore] public IEnumerable<Item?[]?> AllTabs => Enumerable.Range(0, 48).Select(GetTab);
    [JsonIgnore] public IEnumerable<(int Index, Item?[] Tab)> ValidTabs => AllTabs
        .Select((tab, i) => (i, tab))
        .Where(x => x.tab != null)
        .Select(x => (x.i, x.tab!)
    );

    [JsonIgnore] public int SlotsFree => ValidTabs.Sum(tab => GetFreeSlotsInTab(tab.Tab));
    [JsonIgnore] public int SlotsUsed => 42 * ValidTabs.Count() - SlotsFree;

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
    [property: JsonPropertyName("ms")] float MillisecondsRemaining,
    [property: JsonPropertyName("s")] int? S)
{
    public TimeSpan Duration => TimeSpan.FromMilliseconds(MillisecondsRemaining);
    public int StackCount => S ?? 1;
}

public readonly record struct StatusEffects(Dictionary<string, StatusEffect> Effects) {
    public override string ToString() => string.Join(", ", Effects.Keys);

    public StatusEffect? AuthFail => Effects.GetValueOrDefault("authfail");
    public StatusEffect? Blink => Effects.TryGetValue("blink", out StatusEffect eff) ? eff : null;
    public StatusEffect? Block => Effects.TryGetValue("block", out StatusEffect eff) ? eff : null;
    public StatusEffect? Burned => Effects.TryGetValue("burned", out StatusEffect eff) ? eff : null;
    public StatusEffect? Charging => Effects.TryGetValue("charging", out StatusEffect eff) ? eff : null;
    public StatusEffect? Charmed => Effects.TryGetValue("charmed", out StatusEffect eff) ? eff : null;
    public StatusEffect? Cursed =>  Effects.TryGetValue("cursed", out StatusEffect eff) ? eff : null;
    public StatusEffect? Dash => Effects.TryGetValue("dash", out StatusEffect eff) ? eff : null;
    public StatusEffect? Dampened => Effects.TryGetValue("dampened", out StatusEffect eff) ? eff : null;
    public StatusEffect? DarkBlessing => Effects.TryGetValue("darkblessing", out StatusEffect eff) ? eff : null;
    public StatusEffect? DeepFreezed => Effects.TryGetValue("deepfreezed", out StatusEffect eff) ? eff : null;
    public StatusEffect? EBurn => Effects.TryGetValue("eburn", out StatusEffect eff) ? eff : null;
    public StatusEffect? EHeal => Effects.TryGetValue("eheal", out StatusEffect eff) ? eff : null;
    public StatusEffect? Energized => Effects.TryGetValue("energized", out StatusEffect eff) ? eff : null;
    public StatusEffect? EasterLuck => Effects.TryGetValue("easterluck", out StatusEffect eff) ? eff : null;
    public StatusEffect? Fingered => Effects.TryGetValue("fingered", out StatusEffect eff) ? eff : null;
    public StatusEffect? Fishing => Effects.TryGetValue("fishing", out StatusEffect eff) ? eff : null;
    public StatusEffect? Frozen => Effects.TryGetValue("frozen", out StatusEffect eff) ? eff : null;
    public StatusEffect? FullGuard => Effects.TryGetValue("fullguard", out StatusEffect eff) ? eff : null;
    public StatusEffect? FullGuardX => Effects.TryGetValue("fullguardx", out StatusEffect eff) ? eff : null;
    public StatusEffect? Halloween0 => Effects.TryGetValue("halloween0", out StatusEffect eff) ? eff : null;
    public StatusEffect? Halloween1 => Effects.TryGetValue("halloween1", out StatusEffect eff) ? eff : null;
    public StatusEffect? Halloween2 => Effects.TryGetValue("halloween2", out StatusEffect eff) ? eff : null;
    public StatusEffect? HardShell => Effects.TryGetValue("hardshell", out StatusEffect eff) ? eff : null;
    public StatusEffect? HolidaySpirit => Effects.TryGetValue("holidayspirit", out StatusEffect eff) ? eff : null;
    public StatusEffect? HopSickness => Effects.TryGetValue("hopsickness", out StatusEffect eff) ? eff : null;
    public StatusEffect? Invis => Effects.TryGetValue("invis", out StatusEffect eff) ? eff : null;
    public StatusEffect? Invincible => Effects.TryGetValue("invincible", out StatusEffect eff) ? eff : null;
    public StatusEffect? Licenced => Effects.TryGetValue("licenced", out StatusEffect eff) ? eff : null;
    public StatusEffect? Marked => Effects.TryGetValue("marked", out StatusEffect eff) ? eff : null;
    public StatusEffect? MassProduction => Effects.TryGetValue("massproduction", out StatusEffect eff) ? eff : null;
    public StatusEffect? MassProductionPP => Effects.TryGetValue("massproductionpp", out StatusEffect eff) ? eff : null;
    public StatusEffect? MCourage => Effects.TryGetValue("mcourage", out StatusEffect eff) ? eff : null;
    public StatusEffect? MFrenzy => Effects.TryGetValue("mfrenzy", out StatusEffect eff) ? eff : null;
    public StatusEffect? Mining => Effects.TryGetValue("mining", out StatusEffect eff) ? eff : null;
    public StatusEffect? MLifesteal => Effects.TryGetValue("mlifesteal", out StatusEffect eff) ? eff : null;
    public StatusEffect? MLuck => Effects.TryGetValue("mluck", out StatusEffect eff) ? eff : null;
    public StatusEffect? MonsterHunt => Effects.TryGetValue("monsterhunt", out StatusEffect eff) ? eff : null;
    public StatusEffect? MShield => Effects.TryGetValue("mshield", out StatusEffect eff) ? eff : null;
    public StatusEffect? NewcomersBlessing => Effects.TryGetValue("newcomersblessing", out StatusEffect eff) ? eff : null;
    public StatusEffect? NotVerified => Effects.TryGetValue("notverified", out StatusEffect eff) ? eff : null;
    public StatusEffect? PenaltyCD => Effects.TryGetValue("penalty_cd", out StatusEffect eff) ? eff : null;
    public StatusEffect? PhasedOut => Effects.TryGetValue("phasedout", out StatusEffect eff) ? eff : null;
    public StatusEffect? Pickpocket => Effects.TryGetValue("pickpocket", out StatusEffect eff) ? eff : null;
    public StatusEffect? Poisoned => Effects.TryGetValue("poisoned", out StatusEffect eff) ? eff : null;
    public StatusEffect? Poisonous => Effects.TryGetValue("poisonous", out StatusEffect eff) ? eff : null;
    public StatusEffect? Power => Effects.TryGetValue("power", out StatusEffect eff) ? eff : null;
    public StatusEffect? Purifier => Effects.TryGetValue("purifier", out StatusEffect eff) ? eff : null;
    public StatusEffect? Reflection => Effects.TryGetValue("reflection", out StatusEffect eff) ? eff : null;
    public StatusEffect? RSpeed => Effects.TryGetValue("rspeed", out StatusEffect eff) ? eff : null;
    public StatusEffect? Sanguine => Effects.TryGetValue("sanguine", out StatusEffect eff) ? eff : null;
    public StatusEffect? SelfHealing => Effects.TryGetValue("self_healing", out StatusEffect eff) ? eff : null;
    public StatusEffect? Sleeping => Effects.TryGetValue("sleeping", out StatusEffect eff) ? eff : null;
    public StatusEffect? Shocked => Effects.TryGetValue("shocked", out StatusEffect eff) ? eff : null;
    public StatusEffect? Slowness => Effects.TryGetValue("slowness", out StatusEffect eff) ? eff : null;
    public StatusEffect? Stack => Effects.TryGetValue("stack", out StatusEffect eff) ? eff : null;
    public StatusEffect? Stoned => Effects.TryGetValue("stoned", out StatusEffect eff) ? eff : null;
    public StatusEffect? Stunned => Effects.TryGetValue("stunned", out StatusEffect eff) ? eff : null;
    public StatusEffect? SugarRush => Effects.TryGetValue("sugarrush", out StatusEffect eff) ? eff : null;
    public StatusEffect? Town => Effects.TryGetValue("town", out StatusEffect eff) ? eff : null;
    public StatusEffect? Tangled => Effects.TryGetValue("tangled", out StatusEffect eff) ? eff : null;
    public StatusEffect? Withdrawal => Effects.TryGetValue("withdrawal", out StatusEffect eff) ? eff : null;
    public StatusEffect? Woven => Effects.TryGetValue("woven", out StatusEffect eff) ? eff : null;
    public StatusEffect? WarCry => Effects.TryGetValue("warcry", out StatusEffect eff) ? eff : null;
    public StatusEffect? Weakness => Effects.TryGetValue("weakness", out StatusEffect eff) ? eff : null;
    public StatusEffect? XPower => Effects.TryGetValue("xpower", out StatusEffect eff) ? eff : null;
    public StatusEffect? XShotted => Effects.TryGetValue("xshotted", out StatusEffect eff) ? eff : null;
}

public record struct TrackerMax(
    [property: JsonPropertyName("monsters")] Dictionary<string, JsonElement> Monsters)
{
    [JsonIgnore] public readonly Dictionary<string, (int MonsterKillCount, string CharacterName)> MonsterData => Monsters
        .ToDictionary(x => x.Key, x => ((int)x.Value[0].GetDouble(), x.Value[1].GetString()!));
}
