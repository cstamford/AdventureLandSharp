using System.Numerics;
using AdventureLandSharp.Core;
using AdventureLandSharp.Core.SocketApi;
using AdventureLandSharp.Interfaces;
using AdventureLandSharp.Utility;

namespace AdventureLandSharp.Example;

// Implements a basic character that can attack enemies, consume potions, and path through the world.
public class BasicCharacter : ICharacter {
    public BasicCharacter(World world, Socket socket, CharacterClass cls) {
        _cls = cls;
        _world = world;
        _socket = socket;
        _traversal = GetRandomTraversal();

        _btAbility = new Selector(
            // If we're dead, respawn.
            new If(() => Me.Dead, () => _socket.Emit(new Outbound.Respawn())),

            // If we're at very low health, drink a potion if we can, then disconnect.
            new If(() => Me.HealthPercent <= 15, new Selector( 
                new Do(ConsumePotions),
                new Do(() => _running = false))),

            // If we're in the middle of teleporting, let's avoid doing anything else.
            new If(() => _traversal.Edge is MapGraphEdgeTeleport, new Success()),

            // If we need to drink a potion, do it.
            new Do(ConsumePotions),

            // If we have any enemies within attack range, bash them.
            new Do(AttackNearbyEnemy)
        );

        _btMovement = new Selector(
            // If we've finished our path, get a new one.
            new If(() => _traversal.Finished, () => _traversal = GetRandomTraversal()),

            // Update our path.
            new Do(() => _traversal.Update())
        );
    }

    public bool Update() {
        _btAbility.Tick();
        _btMovement.Tick();
        return _running;
    }

    private LocalPlayer Me => _socket.Player;
    private IEnumerable<Monster> Enemies => _socket.Entities.OfType<Monster>();

    private readonly CharacterClass _cls;
    private readonly World _world;
    private readonly Socket _socket;
    private bool _running = true;

    private MapGraphTraversal _traversal;
    private readonly INode _btAbility;
    private readonly INode _btMovement;

    private Cooldown _attackCd = new();
    private Cooldown _healPotionCd = new(TimeSpan.FromSeconds(4));

    private MapGraphTraversal GetRandomTraversal() {
        MapLocation[] interestingGoals = [
            new(_world.GetMap("halloween"), new(8, 630)),
            new(_world.GetMap("main"), new(-1184, 781)),
            new(_world.GetMap("desertland"), new(-669, 315)),
            new(_world.GetMap("winterland"), new(1245, -1490)),
        ];

        return new(_socket, _world.FindRoute(
            new(_world.GetMap(Me.MapName), Me.Position),
            interestingGoals[Random.Shared.Next(interestingGoals.Length)]));
    }

    private Status AttackNearbyEnemy() {
        if (_attackCd.Ready) {
            Monster? target = Enemies
                .Select(x => (monster: x, distance: Vector2.Distance(Me.Position, x.Position)))
                .Where(x => x.distance <= Me.AttackRange)
                .OrderBy(x => x.distance)
                .Select(x => x.monster)
                .FirstOrDefault();

            if (target != null) {
                _socket.Emit(new Outbound.Attack(target.Id));
                _attackCd.Restart(TimeSpan.FromSeconds(Me.AttackSpeed));
                return Status.Success;
            }
        }

        return Status.Fail;
    }

    private Status ConsumePotions() {
        if (_healPotionCd.Ready) {
            int hpSlotId = Me.Inventory.FindSlotId("hpot0");
            int mpSlotId = Me.Inventory.FindSlotId("mpot0");

            int? equipSlotId = null;
            string? useId = null;

            if (Me.HealthPercent < 65 && hpSlotId != -1) {
                equipSlotId = hpSlotId;
            } else if (Me.ManaPercent < 65 && mpSlotId != -1) {
                equipSlotId = mpSlotId;
            } else if (Me.ManaPercent < 90) {
                useId = "mp";
            } else if (Me.HealthPercent < 90) {
                useId = "hp";
            } else if (Me.ManaPercent < 100) {
                useId = "mp";
            } else if (Me.HealthPercent < 100) {
                useId = "hp";
            }

            if (equipSlotId != null) {
                _socket.Emit(new Outbound.Equip(equipSlotId.Value));
                _healPotionCd.Restart(TimeSpan.FromSeconds(2));
                return Status.Success;
            }

            if (useId != null) {
                _socket.Emit(new Outbound.Use(useId));
                _healPotionCd.Restart(TimeSpan.FromSeconds(4));
                return Status.Success;
            }
        }

        return Status.Fail;
    }
}

public record struct Cooldown(TimeSpan cd) {
    public TimeSpan Duration { 
        readonly get => cd;
        set => cd = value;
    }

    public readonly TimeSpan Remaining => _start.Add(cd).Subtract(DateTimeOffset.Now);
    public readonly bool Ready => Remaining <= TimeSpan.Zero;
    
    public void Restart() => _start = DateTimeOffset.Now;
    public void Restart(TimeSpan duration) {
        Duration = duration;
        _start = DateTimeOffset.Now;
    }

    private DateTimeOffset _start = DateTimeOffset.Now;
}
