using AdventureLandSharp.SecretSauce.Character;
using AdventureLandSharp.SecretSauce.Tactics;
using AdventureLandSharp.Core;

namespace AdventureLandSharp.SecretSauce.Strategy;

public class Base_EventsStrategy(CharacterBase me) : StrategyBase(me) {
    public override bool Active => EventMapLocations.Count > 0;
    public override MapLocation TargetMapLocation => EventMapLocations[0];
    public override bool Withdrawing => true;
}
