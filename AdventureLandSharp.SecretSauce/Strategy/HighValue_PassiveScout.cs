using AdventureLandSharp.SecretSauce.Character;
using AdventureLandSharp.SecretSauce.Tactics;
using AdventureLandSharp.Core;

namespace AdventureLandSharp.SecretSauce.Strategy;

public class HighValue_PassiveStrategy(CharacterBase me) : StrategyBase(me) {
    public override bool Active => false;
    public override MapLocation TargetMapLocation => MyLoc;
    public override bool Withdrawing => true;
}
