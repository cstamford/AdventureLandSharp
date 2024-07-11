using AdventureLandSharp.Core.Util;
using System.Diagnostics;
using System.Numerics;
using System.Text.Json;

namespace AdventureLandSharp.Core.SocketApi;

public abstract class Entity {
    public override string ToString() => $"{GetType().Name} [{Name}/{Id}] {Position}";

    public bool Dead => Vitals.Dead || DateTimeOffset.UtcNow < (_likelyDeadUntil ?? DateTimeOffset.MinValue);

    public float Health => Vitals.Hp;
    public float MaxHealth => Vitals.MaxHp;
    public float HealthPercent => Health/MaxHealth * 100;
    public float HealthMissing => MaxHealth - Health;

    public float Mana => Vitals.Mp;
    public float MaxMana => Vitals.MaxMp;
    public float ManaPercent => Mana/MaxMana * 100;
    public float ManaMissing => MaxMana - Mana;

    public float Speed => Stats.Speed;
    public float AttackDamage => Stats.AttackDamage;
    public TimeSpan AttackSpeed => TimeSpan.FromMilliseconds(1000.0 / Stats.AttackFrequency);
    public virtual float AttackRange => Stats.AttackRange;
    public float DPS => (float)(AttackDamage / AttackSpeed.TotalSeconds);

    public virtual Vector2 Size => new(GameConstants.DefaultEntityWidth, GameConstants.DefaultEntityHeight);
    public virtual string Name => _name;

    public string Id { get; protected set; }
    public string Type { get; protected set; }
    public Vector2 Position { get; protected set; }
    public Vector2? GoingPosition { get; protected set; }
    public int Level { get; protected set; }
    public EntityVitals Vitals { get; protected set; }
    public EntityStats Stats { get; protected set; } 
    public StatusEffects StatusEffects { get; protected set; }
    public ISocketEntityMovementPlan? MovementPlan { get; set; }
    public string Target { get; protected set; } = string.Empty;

    public Entity(JsonElement source) {
        Id = source.GetString("id");
        Type = source.GetString("type", Id);
        Position = new(source.GetFloat("x"), source.GetFloat("y"));
        GoingPosition = ParseGoingPosition(source);
        Level = source.GetInt("level", 0);
        Vitals = source.Deserialize<EntityVitals>();
        Stats = source.Deserialize<EntityStats>();
        StatusEffects = new(source.GetProperty("s").Deserialize<Dictionary<string, StatusEffect>>()!);
        Target = source.GetString("target", string.Empty);
        _name = Id;
    }

    public Entity(JsonElement source, GameDataMonster monsterDef) {
        Id = source.GetString("id");
        Type = source.GetString("type", Id);
        Position = new(source.GetFloat("x"), source.GetFloat("y"));
        GoingPosition = ParseGoingPosition(source);
        Level = source.GetInt("level", 0);
        Vitals = new EntityVitals(monsterDef).Update(source);
        Stats = new EntityStats(monsterDef).Update(source);
        StatusEffects = new(source.GetProperty("s").Deserialize<Dictionary<string, StatusEffect>>()!);
        Target = source.GetString("target", string.Empty);
        _name = monsterDef.Name;
    }

    public virtual void Update(JsonElement source) {
        Position = new(source.GetFloat("x"), source.GetFloat("y"));
        GoingPosition = ParseGoingPosition(source);
        Level = source.GetInt("level", Level);
        Vitals = Vitals.Update(source);
        Stats = Stats.Update(source);
        StatusEffects = new(source.GetProperty("s").Deserialize<Dictionary<string, StatusEffect>>()!);
        Target = source.GetString("target", Target);
    }

    public virtual void Tick(float dt) {
        if (Dead) {
            MovementPlan = null;
            return;
        }

        if (GoingPosition.HasValue && (MovementPlan == null || MovementPlan.Position != Position || MovementPlan.Goal != GoingPosition)) {
            MovementPlan = new DestinationMovementPlan(Position, GoingPosition.Value);
        }

        if (!GoingPosition.HasValue) {
            MovementPlan = null;
        }

        if (MovementPlan != null) {
            MovementPlan.Update(dt, Speed);
            Position = MovementPlan.Position;
        }
    }

    public void On(Inbound.ActionData evt) {
        Debug.Assert(evt.TargetId == Id);
        if (Health <= evt.Damage) {
            _likelyDeadUntil = DateTimeOffset.UtcNow.AddMilliseconds(evt.Eta).AddSeconds(1);
        }
    }

    public void On(Inbound.HitData evt) {
        Debug.Assert(evt.TargetId == Id);
        if (evt.Kill) {
            _likelyDeadUntil = DateTimeOffset.UtcNow.AddSeconds(1);
        }
    }

    private readonly string _name;
    private DateTimeOffset? _likelyDeadUntil;

    private static Vector2? ParseGoingPosition(JsonElement source) =>
        source.GetBool("moving", false) &&
        source.TryGetProperty("going_x", out JsonElement x) &&
        source.TryGetProperty("going_y", out JsonElement y) ?
        new(x.GetSingle(), y.GetSingle()) : 
        null;
}

public sealed class Monster(JsonElement source, GameDataMonster monsterDef, Vector2 size) : Entity(source, monsterDef) {
    public GameDataMonster MonsterDef { get; } = monsterDef;
    public Vector2 VisualSize => size;
}

public sealed class Npc : Entity {
    public override string Name => _name;
    public string NpcType { get; private set; }
    public string Focus { get; private set; }

    public Npc(JsonElement source) : base(source) {
        _name = Id[1..];
        NpcType = source.GetString("npc", string.Empty);
        Focus = source.GetString("focus", string.Empty);
    }

    public override void Update(JsonElement source) {
        base.Update(source);
        NpcType = source.GetString("npc", NpcType);
        Focus = source.GetString("focus", Focus);
    }

    private readonly string _name;
}

public class Player(JsonElement source) : Entity(source) {
    public string OwnerId { get; private set; } = source.GetString("owner");
    public override Vector2 Size => new(GameConstants.PlayerWidth, GameConstants.PlayerHeight);
    public PlayerActions Actions { get; private set; } = source.TryGetProperty("c", out JsonElement c) ? c.Deserialize<PlayerActions>() : new();
    public string Skin { get; private set; } = source.GetString("skin", string.Empty);

    public override void Update(JsonElement source) {
        base.Update(source);
        OwnerId = source.GetString("owner");
        Actions = source.TryGetProperty("c", out JsonElement c) ? c.Deserialize<PlayerActions>() : Actions;
        Skin = source.GetString("skin", Skin);
    }

    public override void Tick(float dt) {
        if (Dead) {
            MovementPlan = null;
            return;
        }

        if (MovementPlan != null) {
            MovementPlan.Update(dt, Speed);
            Position = MovementPlan.Position;
        }
    }
}

public sealed class LocalPlayer(JsonElement source) : Player(source) {
    public string MapName { get; private set; } = source.GetString("map");
    public long MapId { get; private set; } = source.GetLong("m");
    public float ExtraRange { get; private set; } = source.GetFloat("xrange", 0);

    public PlayerInventory Inventory { get; private set; } = source.Deserialize<PlayerInventory>();
    public PlayerEquipment Equipment { get; private set; } = source.GetProperty("slots").Deserialize<PlayerEquipment>();
    public PlayerBank? Bank { get; private set; } = ReadPlayerBank(source, null);
    public float CodeCallCost { get; private set; } = source.GetFloat("cc", 0);

    public override float AttackRange => base.AttackRange + ExtraRange;

    // This is the position that the player is currently moving towards.
    public Vector2 GoalPosition => MovementPlan?.Goal ?? Position;

    // This is the last GoalPosition that was sent to the server.
    public Vector2? RemoteGoalPosition { get; set; } = null;

    public override void Update(JsonElement source) {
        base.Update(source);
        MapName = source.GetString("map", MapName);
        MapId = source.GetLong("m", MapId);
        ExtraRange = source.GetFloat("xrange", ExtraRange);
        Inventory = Inventory.Update(source);
        Equipment = source.GetProperty("slots").Deserialize<PlayerEquipment>();
        Bank = ReadPlayerBank(source, Bank);
        CodeCallCost = source.GetFloat("cc", CodeCallCost);
        GoingPosition = null; // we handle this locally, and always ignore remote
    }

    public void On(Inbound.CorrectionData evt) {
        Position = new(evt.X, evt.Y);
    }

    public void On(Inbound.DeathData evt) {
        Debug.Assert(evt.Id == Id);
        Vitals = Vitals with { Dead = true };
    }

    public void On(Inbound.NewMapData evt) {
        MapName = evt.MapName;
        MapId = evt.MapId;
        Position = new(evt.PlayerX, evt.PlayerY);
    }

    private static PlayerBank? ReadPlayerBank(JsonElement source, PlayerBank? bank) {
        if (source.TryGetProperty("user", out JsonElement user) && user.ValueKind == JsonValueKind.Object) {
            return user.Deserialize<PlayerBank>();
        }

        return bank;
    }
}
