using AdventureLandSharp.SecretSauce.Tactics;
using AdventureLandSharp.Core;

namespace AdventureLandSharp.SecretSauce.Strategy;

public class FarmLocationStrategy(MapLocation location) : IStrategy {
    public bool Active => true;
    public MapLocation TargetMapLocation => location;
    public bool Withdrawing => false;
    public void Update() { }
}
