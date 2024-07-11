using AdventureLandSharp.SecretSauce.Character;
using AdventureLandSharp.Core.Util;

namespace AdventureLandSharp.SecretSauce.Tactics;

public class OneEyeAssist(CharacterBase character) : TacticsBase(character) {
    public override bool Active => !MyChar.Withdrawing && OneEyes.Any();

    public override CachedMonster? AttackTarget => OneEyes
        .Where(x => x.Monster.Target != string.Empty)
        .OrderByDescending(x => x.Monster.Health)
        .FirstOrNull();

    public override IPositioningPlan PositioningPlan => new MeleePositioningPlan(MyChar);

    private IEnumerable<CachedMonster> OneEyes => Enemies.Where(x => x.Type == "oneeye");
}
