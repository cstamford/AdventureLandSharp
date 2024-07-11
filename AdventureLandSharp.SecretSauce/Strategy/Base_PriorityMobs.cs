using AdventureLandSharp.SecretSauce.Character;
using AdventureLandSharp.SecretSauce.Tactics;
using AdventureLandSharp.Core;

namespace AdventureLandSharp.SecretSauce.Strategy;

public class Base_PriorityMobsStrategy(CharacterBase me) : StrategyBase(me) {
    public override bool Active => PriorityMobHuntEvent.HasValue;
    public override MapLocation TargetMapLocation => PriorityMobHuntEvent!.Value.MobLocation;
    public override bool Withdrawing => true;
}
