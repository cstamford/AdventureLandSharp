using AdventureLandSharp.Util;
using System.Text.Json.Serialization;

namespace AdventureLandSharp.Socket;

public class ServerToClientTypes {
    public record struct StatusEffect(
        [property: JsonPropertyName("ms")] double Ms,
        [property: JsonPropertyName("f")] string? F,
        [property: JsonPropertyName("ability")] bool? Ability,
        [property: JsonPropertyName("strong")] bool? Strong);

    public record struct Slot(
        [property: JsonPropertyName("level")] double Level,
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("gift")] double? Gift,
        [property: JsonPropertyName("stat_type")] string? StatType,
        [property: JsonPropertyName("acc")] double? Acc,
        [property: JsonPropertyName("ach")] string? Ach,
        [property: JsonPropertyName("price")] double? Price,
        [property: JsonPropertyName("q")] double? Q,
        [property: JsonPropertyName("rid")] string? Rid);

    public record struct Player(
        [property: JsonPropertyName("hp")] double Hp,
        [property: JsonPropertyName("max_hp")] double MaxHp,
        [property: JsonPropertyName("mp")] double Mp,
        [property: JsonPropertyName("max_mp")] double MaxMp,
        [property: JsonPropertyName("xp")] double Xp,
        [property: JsonPropertyName("attack")] double Attack,
        [property: JsonPropertyName("heal")] double Heal,
        [property: JsonPropertyName("frequency")] double Frequency,
        [property: JsonPropertyName("speed")] double Speed,
        [property: JsonPropertyName("range")] double Range,
        [property: JsonPropertyName("armor")] double Armor,
        [property: JsonPropertyName("resistance")] double Resistance,
        [property: JsonPropertyName("level")] double Level,
        [property: JsonPropertyName("party")] string? Party,
        [property: JsonPropertyName("rip"), JsonConverter(typeof(JsonBoolOrIntConverter))] bool Rip,
        [property: JsonPropertyName("afk"), JsonConverter(typeof(JsonBoolOrStringConverter))] bool Afk,
        [property: JsonPropertyName("target")] string Target,
        [property: JsonPropertyName("s")] Dictionary<string, StatusEffect> S,
        [property: JsonPropertyName("c")] Dictionary<string, object> C,
        [property: JsonPropertyName("q")] Dictionary<string, object> Q,
        [property: JsonPropertyName("age")] double Age,
        [property: JsonPropertyName("pdps")] double Pdps,
        [property: JsonPropertyName("id")] string Id,
        [property: JsonPropertyName("x")] double X,
        [property: JsonPropertyName("y")] double Y,
        [property: JsonPropertyName("moving")] bool Moving,
        [property: JsonPropertyName("going_x")] double GoingX,
        [property: JsonPropertyName("going_y")] double GoingY,
        [property: JsonPropertyName("abs")] bool Abs,
        [property: JsonPropertyName("move_num")] double MoveNum,
        [property: JsonPropertyName("angle")] double Angle,
        [property: JsonPropertyName("cid")] double Cid,
        [property: JsonPropertyName("controller")] string? Controller,
        [property: JsonPropertyName("skin")] string Skin,
        [property: JsonPropertyName("cx")] Dictionary<string, object> Cx,
        [property: JsonPropertyName("slots")] Dictionary<string, Slot?> Slots,
        [property: JsonPropertyName("ctype")] string Ctype,
        [property: JsonPropertyName("owner")] string Owner,
        [property: JsonPropertyName("double")] double? Int,
        [property: JsonPropertyName("str")] double? Str,
        [property: JsonPropertyName("dex")] double? Dex,
        [property: JsonPropertyName("vit")] double? Vit,
        [property: JsonPropertyName("for")] double? For,
        [property: JsonPropertyName("mp_cost")] double? MpCost,
        [property: JsonPropertyName("mp_reduction")] double? MpReduction,
        [property: JsonPropertyName("max_xp")] double? MaxXp,
        [property: JsonPropertyName("goldm")] double? GoldM,
        [property: JsonPropertyName("xpm")] double? Xpm,
        [property: JsonPropertyName("luckm")] double? LuckM,
        [property: JsonPropertyName("map")] string? Map,
        [property: JsonPropertyName("in")] string? In,
        [property: JsonPropertyName("isize")] double? Isize,
        [property: JsonPropertyName("esize")] double? Esize,
        [property: JsonPropertyName("gold")] double? Gold,
        [property: JsonPropertyName("cash")] double? Cash,
        [property: JsonPropertyName("targets")] double? Targets,
        [property: JsonPropertyName("m")] long? MapId,
        [property: JsonPropertyName("evasion")] double? Evasion,
        [property: JsonPropertyName("miss")] double? Miss,
        [property: JsonPropertyName("reflection")] double? Reflection,
        [property: JsonPropertyName("lifesteal")] double? Lifesteal,
        [property: JsonPropertyName("manasteal")] double? Manasteal,
        [property: JsonPropertyName("rpiercing")] double? Rpiercing,
        [property: JsonPropertyName("apiercing")] double? Apiercing,
        [property: JsonPropertyName("crit")] double? Crit,
        [property: JsonPropertyName("critdamage")] double? Critdamage,
        [property: JsonPropertyName("dreturn")] double? Dreturn,
        [property: JsonPropertyName("tax")] double? Tax,
        [property: JsonPropertyName("xrange")] double? Xrange,
        [property: JsonPropertyName("pnresistance")] double? Pnresistance,
        [property: JsonPropertyName("firesistance")] double? Firesistance,
        [property: JsonPropertyName("fzresistance")] double? Fzresistance,
        [property: JsonPropertyName("phresistance")] double? Phresistance,
        [property: JsonPropertyName("stresistance")] double? Stresistance,
        [property: JsonPropertyName("incdmgamp")] double? Incdmgamp,
        [property: JsonPropertyName("stun")] double? Stun,
        [property: JsonPropertyName("blast")] double? Blast,
        [property: JsonPropertyName("explosion")] double? Explosion,
        [property: JsonPropertyName("courage")] double? Courage,
        [property: JsonPropertyName("mcourage")] double? Mcourage,
        [property: JsonPropertyName("pcourage")] double? Pcourage,
        [property: JsonPropertyName("fear")] double? Fear,
        [property: JsonPropertyName("items")] List<Slot?>? Items,
        [property: JsonPropertyName("cc")] double? Cc,
        [property: JsonPropertyName("ipass")] string? Ipass,
        [property: JsonPropertyName("home")] string? Home,
        [property: JsonPropertyName("friends")] List<string>? Friends,
        [property: JsonPropertyName("acx")] Dictionary<string, object>? Acx,
        [property: JsonPropertyName("xcx")] List<object>? Xcx,
        [property: JsonPropertyName("emx")] Dictionary<string, object>? Emx,
        [property: JsonPropertyName("info")] Dictionary<string, object>? Info,
        [property: JsonPropertyName("base_gold")] Dictionary<string, Dictionary<string, double>>? BaseGold,
        [property: JsonPropertyName("s_info")] Dictionary<string, object>? SInfo,
        [property: JsonPropertyName("direction")] double? Direction);

    public record struct Monster(
        [property: JsonPropertyName("hp")] double Hp,
        [property: JsonPropertyName("resistance")] double Resistance,
        [property: JsonPropertyName("id")] string Id,
        [property: JsonPropertyName("x")] double X,
        [property: JsonPropertyName("y")] double Y,
        [property: JsonPropertyName("moving")] bool Moving,
        [property: JsonPropertyName("going_x")] double GoingX,
        [property: JsonPropertyName("going_y")] double GoingY,
        [property: JsonPropertyName("abs")] bool Abs,
        [property: JsonPropertyName("move_num")] double MoveNum,
        [property: JsonPropertyName("angle")] double Angle,
        [property: JsonPropertyName("type")] string Type,
        [property: JsonPropertyName("cid")] double Cid,
        [property: JsonPropertyName("s")] Dictionary<string, StatusEffect> S,
        [property: JsonPropertyName("armor")] double? Armor,
        [property: JsonPropertyName("speed")] double? Speed,
        [property: JsonPropertyName("mp")] double? Mp,
        [property: JsonPropertyName("target")] string? Target);

    public record struct ServerDetails(
        [property: JsonPropertyName("schedule")] Schedule Schedule,
        [property: JsonPropertyName("lunarnewyear")] bool LunarNewYear,
        [property: JsonPropertyName("dragold")] DragoldEvent Dragold
    );

    public record struct Schedule(
        [property: JsonPropertyName("time_offset")] double TimeOffset,
        [property: JsonPropertyName("dailies")] List<double> Dailies,
        [property: JsonPropertyName("nightlies")] List<double> Nightlies,
        [property: JsonPropertyName("night")] bool Night
    );

    public record struct DragoldEvent(
        [property: JsonPropertyName("live")] bool Live,
        [property: JsonPropertyName("spawn")] DateTime Spawn
    );
}

public class ServerToClient {
    public record struct Action(
        [property: JsonPropertyName("attacker")] string Attacker,
        [property: JsonPropertyName("target")] string Target,
        [property: JsonPropertyName("type")] string Type,
        [property: JsonPropertyName("source")] string Source,
        [property: JsonPropertyName("x")] double X,
        [property: JsonPropertyName("y")] double Y,
        [property: JsonPropertyName("eta")] double Eta,
        [property: JsonPropertyName("m")] double M,
        [property: JsonPropertyName("pid")] string Pid,
        [property: JsonPropertyName("projectile")] string Projectile,
        [property: JsonPropertyName("damage")] double Damage);

    public record struct Chat(
        [property: JsonPropertyName("owner")] string Owner,
        [property: JsonPropertyName("message")] string Message);

    public record struct Correction(
        [property: JsonPropertyName("x")] double X,
        [property: JsonPropertyName("y")] double Y);

    public record struct Death(
        [property: JsonPropertyName("id")] string Id,
        [property: JsonPropertyName("luckm")] double LuckModifier);

    public record struct Disappear(
        [property: JsonPropertyName("id")] string Id,
        [property: JsonPropertyName("reason")] string Reason,
        [property: JsonPropertyName("to")] string To,
        [property: JsonPropertyName("s")] object S);

    public record struct Entities(
        [property: JsonPropertyName("players")] List<ServerToClientTypes.Player> Players,
        [property: JsonPropertyName("monsters")] List<ServerToClientTypes.Monster> Monsters,
        [property: JsonPropertyName("type")] string Type,
        [property: JsonPropertyName("in")] string In,
        [property: JsonPropertyName("map")] string Map);

    public record struct GameEvent(
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("map")] string Map,
        [property: JsonPropertyName("x")] double X,
        [property: JsonPropertyName("y")] double Y);

    public record struct Hit(
        [property: JsonPropertyName("hid")] string Hid,
        [property: JsonPropertyName("source")] string Source,
        [property: JsonPropertyName("projectile")] string Projectile,
        [property: JsonPropertyName("damage_type")] string DamageType,
        [property: JsonPropertyName("pid")] string Pid,
        [property: JsonPropertyName("id")] string Id,
        [property: JsonPropertyName("damage")] double Damage,
        [property: JsonPropertyName("lifesteal")] double Lifesteal,
        [property: JsonPropertyName("kill")] bool? Kill);

    public record struct ServerMessage(
        [property: JsonPropertyName("message")] string Message,
        [property: JsonPropertyName("color")] string Color,
        [property: JsonPropertyName("type")] string? Type,
        [property: JsonPropertyName("item")] ServerToClientTypes.Slot? Item,
        [property: JsonPropertyName("name")] string? Name,
        [property: JsonPropertyName("event")] bool? Event,
        [property: JsonPropertyName("discord")] string? Discord);

    public record struct Upgrade(
        [property: JsonPropertyName("type")] string Type,
        [property: JsonPropertyName("success")] double Success);

    public record struct Welcome(
        [property: JsonPropertyName("region")] string Region,
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("pvp")] bool Pvp,
        [property: JsonPropertyName("gameplay")] string Gameplay,
        [property: JsonPropertyName("info")] Dictionary<string, object> Info,
        [property: JsonPropertyName("version")] double Version,
        [property: JsonPropertyName("x")] double X,
        [property: JsonPropertyName("y")] double Y,
        [property: JsonPropertyName("map")] string Map,
        [property: JsonPropertyName("in")] string In,
        [property: JsonPropertyName("S")] ServerToClientTypes.ServerDetails S
    );
}
