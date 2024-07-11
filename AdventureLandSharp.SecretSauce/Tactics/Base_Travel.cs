using AdventureLandSharp.SecretSauce.Character;
using AdventureLandSharp.Core.Util;

namespace AdventureLandSharp.SecretSauce.Tactics;

public class Base_TravelTactics(CharacterBase me) : TacticsBase(me) {
    public override bool Active => true;
    public override CachedMonster? AttackTarget => EnemiesInRange.FirstOrNull(x => x.PriorityType == TargetPriorityType.Opportunistic);
    public override IPositioningPlan PositioningPlan => new NullPositioningPlan(MyChar);
}

