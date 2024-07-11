using AdventureLandSharp.SecretSauce.Tactics;
using AdventureLandSharp.Core;

namespace AdventureLandSharp.SecretSauce.Strategy;

public class DynamicStrategy(Func<bool> isActive, Func<MapLocation> targetMapLocation) : IStrategy {
    public bool Active => isActive();
    public MapLocation TargetMapLocation => targetMapLocation();
    public bool Withdrawing => true;
    public void Update() { }
}
