using AdventureLandSharp.SecretSauce.Character;
using AdventureLandSharp.SecretSauce.Tactics;
using AdventureLandSharp.Core;

namespace AdventureLandSharp.SecretSauce.Strategy;

public class HighValue_RespawnMobScout(
    CharacterBase me,
    string type,
    TimeSpan respawnTime,
    List<MapLocation> locations) : StrategyBase(me) {

    public override bool Active => DateTimeOffset.UtcNow >= _nextSpawn && _unvisitedLocations.Count > 0;
    public override MapLocation TargetMapLocation => _unvisitedLocations[0];
    public override bool Withdrawing => true;
    public override void Update() {
        if (Active && Enemies.Any(x => x.Type == type)) {
            Reset(respawnTime.Add(TimeSpan.FromSeconds(60)));
        }

        if (Active && MyLoc.Equivalent(TargetMapLocation)) {
            _unvisitedLocations.RemoveAt(0);
            if (_unvisitedLocations.Count == 0) {
                Reset(TimeSpan.FromMinutes(15));
            }
        }
    }

    private DateTimeOffset _nextSpawn = DateTimeOffset.UtcNow.AddSeconds(Random.Shared.Next(1, 15));
    private List<MapLocation> _unvisitedLocations = [..locations];

    private void Reset(TimeSpan time) {
        _nextSpawn = DateTimeOffset.UtcNow.Add(time);
        _unvisitedLocations = [..locations];
        Log.Info($"Next check for {type} at {_nextSpawn}"); 
    }
}
