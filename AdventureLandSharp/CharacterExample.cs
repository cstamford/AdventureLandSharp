using AdventureLandSharp.Core.Util;

namespace AdventureLandSharp;

public class CharacterFactoryExample : ICharacterFactory
{
    public ICharacter Create(CharacterClass cls, World world, Socket socket)
    {
        return new CharacterExample(cls, world, socket);
    }
}

public class CharacterExample : ICharacter
{
    private readonly INode _btAbility;
    private readonly INode _btMovement;

    private readonly CharacterClass _cls;
    private readonly Socket _socket;
    private readonly World _world;

    private Cooldown _attackCd;
    private Cooldown _healPotionCd = new(TimeSpan.FromSeconds(4));
    private bool _running = true;

    private GraphTraversal _traversal;

    public CharacterExample(CharacterClass cls, World world, Socket socket)
    {
        _cls = cls;
        _world = world;
        _socket = socket;
        _traversal = GetRandomTraversal();

        _btAbility = new Selector(
            // If we're dead, respawn.
            new If(() => Me.Dead, async () => await _socket.Emit(new Outbound.Respawn())),

            // If we're at very low health, trigger a disconnect.
            new If(() => Me.HealthPercent <= 15, () => Task.FromResult(_running = false)),

            // If we're in the middle of teleporting, let's avoid doing anything else.
            new If(() => _traversal.Edge is MapGraphEdgeTeleport, new Success()),

            // If we need to drink a potion, do it.
            new Do(ConsumePotions),

            // If we have any enemies within attack range, bash them.
            new Do(AttackNearbyEnemy)
        );

        _btMovement = new Selector(
            // If we've finished our path, get a new one.
            new If(() => _traversal.Finished, () => Task.FromResult(_traversal = GetRandomTraversal())),

            // Update our path.
            new Do(() =>
            {
                _traversal.Update();
                return Task.CompletedTask;
            })
        );
    }

    private LocalPlayer Me => _socket.Player;
    private IEnumerable<Monster> Enemies => _socket.Entities.OfType<Monster>();

    public async Task<bool> Update(float dt)
    {
        await _btAbility.Tick();
        await _btMovement.Tick();
        return _running;
    }

    private GraphTraversal GetRandomTraversal()
    {
        var i = 1 + 1;
        
        MapLocation[] interestingGoals =
        [
            new MapLocation(_world.GetMap("halloween"), new Vector2(8, 630)),
            new MapLocation(_world.GetMap("main"), new Vector2(-1184, 781)),
            new MapLocation(_world.GetMap("desertland"), new Vector2(-669, 315)),
            new MapLocation(_world.GetMap("winterland"), new Vector2(1245, -1490))
        ];

        var map = _world.GetMap(Me.MapName);
        var location = new MapLocation(map, Me.Position);
        var goal = interestingGoals[Random.Shared.Next(interestingGoals.Length)];
        var route = _world.FindRoute(location, goal);

        return new GraphTraversal(_socket, route);
    }

    private async Task<Status> AttackNearbyEnemy()
    {
        if (!_attackCd.Ready) return Status.Fail;

        var target = Enemies
            .Select(x => (monster: x, distance: Vector2.Distance(Me.Position, x.Position)))
            .Where(x => x.distance <= Me.AttackRange)
            .OrderBy(x => x.distance)
            .Select(x => x.monster)
            .FirstOrDefault();

        if (target == null) return Status.Fail;

        await _socket.Emit(new Outbound.Attack(target.Id));
        _attackCd.Restart(TimeSpan.FromSeconds(Me.AttackSpeed));
        return Status.Success;
    }

    private async Task<Status> ConsumePotions()
    {
        if (!_healPotionCd.Ready) return Status.Fail;

        var hpSlotId = Me.Inventory.FindSlotId("hpot0");
        var mpSlotId = Me.Inventory.FindSlotId("mpot0");

        int? equipSlotId = null;
        string? useId = null;

        if (Me.HealthPercent < 65 && hpSlotId != -1)
            equipSlotId = hpSlotId;
        else if (Me.ManaPercent < 65 && mpSlotId != -1)
            equipSlotId = mpSlotId;
        else if (Me.ManaPercent < 90)
            useId = "mp";
        else if (Me.HealthPercent < 90)
            useId = "hp";
        else if (Me.ManaPercent < 100)
            useId = "mp";
        else if (Me.HealthPercent < 100) useId = "hp";

        if (equipSlotId != null)
        {
            await _socket.Emit(new Outbound.Equip(equipSlotId.Value));
            _healPotionCd.Restart(TimeSpan.FromSeconds(2));
            return Status.Success;
        }

        if (useId == null) return Status.Fail;

        await _socket.Emit(new Outbound.Use(useId));
        _healPotionCd.Restart(TimeSpan.FromSeconds(4));
        return Status.Success;
    }
}

public record struct Cooldown(TimeSpan cd)
{
    private DateTimeOffset _start = DateTimeOffset.Now;

    public TimeSpan Duration
    {
        readonly get => cd;
        set => cd = value;
    }

    public readonly TimeSpan Remaining => _start.Add(cd).Subtract(DateTimeOffset.Now);
    public readonly bool Ready => Remaining <= TimeSpan.Zero;

    public void Restart()
    {
        _start = DateTimeOffset.Now;
    }

    public void Restart(TimeSpan duration)
    {
        Duration = duration;
        _start = DateTimeOffset.Now;
    }
}