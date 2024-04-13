namespace AdventureLandSharp.Core.SocketApi;

public abstract class Entity
{
    private readonly string _name;

    public Entity(JsonElement source)
    {
        Id = source.GetString("id");
        Position = new Vector2(source.GetFloat("x"), source.GetFloat("y"));
        GoingPosition = ParseGoingPosition(source);
        Vitals = source.Deserialize<EntityVitals>();
        Stats = source.Deserialize<EntityStats>();
        StatusEffects = source.GetProperty("s").Deserialize<StatusEffects>();
        _name = Id;
    }

    public Entity(JsonElement source, GameDataMonster monsterDef)
    {
        Id = source.GetString("id");
        Position = new Vector2(source.GetFloat("x"), source.GetFloat("y"));
        GoingPosition = ParseGoingPosition(source);
        Vitals = new EntityVitals(monsterDef).Update(source);
        Stats = new EntityStats(monsterDef).Update(source);
        StatusEffects = source.GetProperty("s").Deserialize<StatusEffects>();
        _name = monsterDef.Name;
    }

    public bool Dead => Vitals.Dead;

    public float Health => Vitals.Hp;
    public float MaxHealth => Vitals.MaxHp;
    public float HealthPercent => Health / MaxHealth * 100;

    public float Mana => Vitals.Mp;
    public float MaxMana => Vitals.MaxMp;
    public float ManaPercent => Mana / MaxMana * 100;

    public float Speed => Stats.Speed;
    public float AttackDamage => Stats.AttackDamage;
    public float AttackSpeed => Stats.AttackFrequency;
    public float AttackRange => Stats.AttackRange;
    public float DPS => AttackDamage / AttackSpeed;

    public virtual string Name => _name;
    public string Id { get; protected set; }
    public Vector2 Position { get; protected set; }
    public Vector2? GoingPosition { get; protected set; }
    public EntityVitals Vitals { get; protected set; }
    public EntityStats Stats { get; protected set; }
    public StatusEffects StatusEffects { get; protected set; }
    public ISocketEntityMovementPlan? MovementPlan { get; set; }

    public virtual void Update(JsonElement source)
    {
        Position = new Vector2(source.GetFloat("x"), source.GetFloat("y"));
        GoingPosition = ParseGoingPosition(source);
        Vitals = Vitals.Update(source);
        Stats = Stats.Update(source);
        StatusEffects = source.GetProperty("s").Deserialize<StatusEffects>();
    }

    public virtual void Tick(float dt)
    {
        if (Dead)
        {
            MovementPlan = null;
            return;
        }

        if (GoingPosition.HasValue && (MovementPlan == null || MovementPlan.Position != Position ||
                                       MovementPlan.Goal != GoingPosition))
            MovementPlan = new DestinationMovementPlan(Position, GoingPosition.Value);

        if (MovementPlan != null)
        {
            var finished = MovementPlan.Update(dt, Speed);
            Position = MovementPlan.Position;

            if (finished) MovementPlan = null;
        }
    }

    public void On(Inbound.DeathData evt)
    {
        Debug.Assert(evt.Id == Id);
        Vitals = Vitals with {Dead = true};
    }

    private static Vector2? ParseGoingPosition(JsonElement source)
    {
        return source.GetBool("moving", false) &&
               source.TryGetProperty("going_x", out var x) &&
               source.TryGetProperty("going_y", out var y)
            ? new Vector2(x.GetSingle(), y.GetSingle())
            : null;
    }
}

public sealed class Monster(JsonElement source, GameDataMonster monsterDef) : Entity(source, monsterDef);

public sealed class Npc(JsonElement source) : Entity(source)
{
    public override string Name => Id[1..];
}

public class Player(JsonElement source) : Entity(source)
{
    public string OwnerId { get; private set; } = source.GetString("owner");

    public override void Update(JsonElement source)
    {
        base.Update(source);
        OwnerId = source.GetString("owner");
    }
}

public sealed class LocalPlayer(JsonElement source) : Player(source)
{
    public Vector2 GoalPosition => MovementPlan?.Goal ?? Position;
    public string MapName { get; private set; } = source.GetString("map");
    public long MapId { get; private set; } = source.GetLong("m");

    public PlayerInventory Inventory { get; private set; } = source.Deserialize<PlayerInventory>();
    public PlayerEquipment Equipment { get; private set; } = source.GetProperty("slots").Deserialize<PlayerEquipment>();
    public Vector2 RemotePosition { get; set; } = new(source.GetFloat("x"), source.GetFloat("y"));

    public override void Update(JsonElement source)
    {
        base.Update(source);
        Inventory = Inventory.Update(source);
        Equipment = source.GetProperty("slots").Deserialize<PlayerEquipment>();
        RemotePosition = new Vector2(source.GetFloat("x"), source.GetFloat("y"));
        GoingPosition = null; // we handle this locally, and always ignore remote
    }

    public void On(Inbound.CorrectionData evt)
    {
        Position = new Vector2(evt.X, evt.Y);
        MovementPlan = null;
    }

    public void On(Inbound.NewMapData evt)
    {
        MapName = evt.MapName;
        MapId = evt.MapId;
        Position = new Vector2(evt.PlayerX, evt.PlayerY);
        MovementPlan = null;
    }
}