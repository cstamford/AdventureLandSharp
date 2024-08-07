using System.Diagnostics;
using AdventureLandSharp.Core;
using AdventureLandSharp.Core.SocketApi;
using AdventureLandSharp.Core.Util;
using AdventureLandSharp.Helpers;
using AdventureLandSharp.Interfaces;
using AdventureLandSharp.Utility;

namespace AdventureLandSharp.Example;

// Implements a basic character that can attack enemies, consume potions, and path through the world.
public class BasicCharacter : ICharacter {
    public Socket Socket => _socket;
    public CharacterClass Class => _cls;
    public LocalPlayer Entity => _socket.Player;
    public MapLocation EntityLocation => new(_world.GetMap(Entity.MapName), Entity.Position);

    public BasicCharacter(World world, Socket socket, CharacterClass cls) {
        _cls = cls;
        _world = world;
        _socket = socket;
        _traversal = GetRandomTraversal();

        _btAbility = new Selector(
            // If we're dead, respawn.
            new If(() => Me.Dead, () => _socket.Emit<Outbound.Respawn>(new())),

            // If we're at very low health, drink a potion if we can, then disconnect.
            new If(() => Me.HealthPercent <= 15, new Selector( 
                ConsumePotions(),
                new Do(() => _running = false))),

            // If we're in the middle of teleporting, let's avoid doing anything else.
            new If(() => _traversal.CurrentEdge is MapGraphEdgeTeleport, new Success()),

            // If we need to drink a potion, do it.
            ConsumePotions(),

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

    private LocalPlayer Me => Entity;

    private IEnumerable<Monster> Enemies => _socket.Entities.OfType<Monster>();

    private readonly CharacterClass _cls;
    private readonly World _world;
    private readonly Socket _socket;
    private bool _running = true;

    private MapGraphTraversal _traversal;
    private readonly Selector _btAbility;
    private readonly Selector _btMovement;

    private readonly Cooldown _attackCd = new(default);

    private MapGraphTraversal GetRandomTraversal() {
        MapLocation[] interestingGoals = [
            new(_world.GetMap("halloween"), new(8, 630)),
            new(_world.GetMap("main"), new(-1184, 781)),
            new(_world.GetMap("desertland"), new(-669, 315)),
            new(_world.GetMap("winterland"), new(1245, -1490)),
        ];

        MapLocation start = new(_world.GetMap(Me.MapName), Me.Position);
        MapLocation end = interestingGoals[Random.Shared.Next(interestingGoals.Length)];
        IEnumerable<IMapGraphEdge> edges = _world.FindRoute(start, end);
        Debug.Assert(edges.Any() || start == end, "No route found: the code will cope with this, but create a unit test and investigate.");

        return new(_socket, edges, end);
    }

    private Status AttackNearbyEnemy() {
        if (_attackCd.Ready) {
            Monster? target = Enemies
                .Select(x => (monster: x, distance: Me.Dist(x)))
                .Where(x => x.distance <= Me.AttackRange)
                .OrderBy(x => x.distance)
                .Select(x => x.monster)
                .FirstOrDefault();

            if (target != null) {
                _socket.Emit<Outbound.Attack>(new(target.Id));
                _attackCd.Duration = Me.AttackSpeed;
                _attackCd.Restart();
                return Status.Success;
            }
        }

        return Status.Fail;
    }

    private ConsumePotionsNode ConsumePotions() => new(() => (_socket, Me));
}
