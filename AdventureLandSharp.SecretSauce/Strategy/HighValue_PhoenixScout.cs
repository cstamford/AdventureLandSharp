using AdventureLandSharp.SecretSauce.Character;
using AdventureLandSharp.SecretSauce.Tactics;
using AdventureLandSharp.Core;
using AdventureLandSharp.Core.Util;
using AdventureLandSharp.Interfaces;

namespace AdventureLandSharp.SecretSauce.Strategy;

public class HighValue_PhoenixScoutStrategy : StrategyBase {
    public HighValue_PhoenixScoutStrategy(CharacterBase me) : base(me) {
        Socket.OnDeath += evt => {
            if (Enemies.FirstOrNull(x => x.Id == evt.Id)?.Type == "phoenix") {
                Reset(_respawnTime);
                EventBusHandle.Emit<PhoenixDiedEvent>(new());
            }
        };

        EventBusHandle.Register<PhoenixDiedEvent>(evt => Reset(_respawnTime));
        EventBusHandle.Register<PhoenixSpawnCheckedEvent>(evt => _unvisitedLocations.Remove(evt.Location));
        EventBusHandle.Register<PhoenixLocatedEvent>(evt => Reset(_killPlusRespawnTime));

        Reset(TimeSpan.FromSeconds(0));
    }

    public override bool Active => DateTimeOffset.UtcNow >= _nextPhoenix;
    public override MapLocation TargetMapLocation => _unvisitedLocations.FirstOrNull() ?? MyLoc;
    public override bool Withdrawing => true;
    public override void Update() {
        CachedMonster? phoenix = Enemies.FirstOrNull(x => x.Type == "phoenix");

        if (phoenix.HasValue && _phoenixId != phoenix.Value.Id) {
            EventBusHandle.Emit<PhoenixLocatedEvent>(new());
            _phoenixId = phoenix.Value.Id;
            _unvisitedLocations.Clear();
        }

        if (_unvisitedLocations.Count > 0 && MyLoc.Equivalent(TargetMapLocation)) {
            EventBusHandle.Emit<PhoenixSpawnCheckedEvent>(new(TargetMapLocation));
            _unvisitedLocations.RemoveAt(0);
        }

        if (_unvisitedLocations.Count == 0) {
            Reset(_killPlusRespawnTime);
        }
    }

    private DateTimeOffset _nextPhoenix;
    private List<MapLocation> _unvisitedLocations = [];
    private string _phoenixId = string.Empty;

    private void Reset(TimeSpan time) {
        _nextPhoenix = DateTimeOffset.UtcNow.Add(time);
        _unvisitedLocations = [..PositionsToVisit]; 
    }

    private IEnumerable<MapLocation> PositionsToVisit => MyChar.Class switch {
        CharacterClass.Mage => [CrocSpawn, ScorpSpawn, HalloweenSpawn, BeachSpawn, CaveSpawn],
        CharacterClass.Merchant => [CaveSpawn, BeachSpawn],
        _ => [BeachSpawn]
    };

    private MapLocation BeachSpawn => new(World.GetMap("main"), new(-984, 1762));
    private MapLocation CrocSpawn => new(World.GetMap("main"), new(350, 1550));
    private MapLocation ScorpSpawn => new(World.GetMap("main"), new(1500, -375));
    private MapLocation CaveSpawn => new(World.GetMap("cave"), new(-397, -1239));
    private MapLocation HalloweenSpawn => new(World.GetMap("halloween"), new(35, 600));

    private static readonly TimeSpan _respawnTime = TimeSpan.FromSeconds(32);
    private static readonly TimeSpan _killPlusRespawnTime = TimeSpan.FromMinutes(1);
}

public record struct PhoenixDiedEvent() : ISessionEvent;
public record struct PhoenixSpawnCheckedEvent(MapLocation Location) : ISessionEvent;
public record struct PhoenixLocatedEvent() : ISessionEvent;