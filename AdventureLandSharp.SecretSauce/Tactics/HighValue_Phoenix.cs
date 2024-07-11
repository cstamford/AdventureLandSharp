using System.Diagnostics;
using AdventureLandSharp.SecretSauce.Character;
using AdventureLandSharp.Core;
using AdventureLandSharp.Core.Util;
using AdventureLandSharp.Interfaces;

namespace AdventureLandSharp.SecretSauce.Tactics;

public class HighValue_PhoenixTactics(CharacterBase me) : TacticsBase(me) {
    public override bool Active => 
        !MyChar.Withdrawing &&
        Cfg.GetTargetPriorityType("phoenix") == TargetPriorityType.Priority &&
        AttackTarget.HasValue;

    public override CachedMonster? AttackTarget => Enemies.FirstOrNull(x => x.Type == "phoenix");
    public override IPositioningPlan PositioningPlan => new PhoenixPositioningPlan(MyChar);
    public override void Update() {
        Debug.Assert(AttackTarget.HasValue);

        if (MyChar.Class == CharacterClass.Mage) { /* TODO calc highest luck */
            return;
        }

        if (AttackTarget.Value.Monster.Target != Me.Name) {
            return;
        }

        if (AttackTarget.Value.Monster.HealthPercent >= 5) {
            return;
        }

        // Don't quit unless we have an ally nearby hitting it
        if (PartyPlayers.All(x => x.Player.Target != AttackTarget.Value.Id)) {
            return;
        }

        // Don't quit if any non-party players are nearby and hitting it.
        if (Players.Any(x => x.Player.Target == AttackTarget.Value.Id && !PartyPlayers.Any(y => y.Player.Name == x.Name))) {
            return;
        }

        Log.Info($"Jailing to drop Phoenix agro");
        MyChar.ForceJail();
    }
}

public sealed class PhoenixPositioningPlan(CharacterBase me) : PositioningPlan(me) {
    public override GridWeight GetPosition(IReadOnlyList<GridWeight> weights) {
        CachedNpc? kane = Npcs.FirstOrNull(x => x.Name == "Kane");
        if (BestAttackTarget.Monster.Target == Me.Name && kane.HasValue && !kane.Value.InAuraRange(MyLoc.Position)) {
            return new GridWeight(kane.Value.Position.Grid(MyLoc.Map), new MeleePositioningWeights());
        }

        return _melee.GetPosition(weights);
    }

    public override void StoreWeights(List<GridWeight> weights) => _melee.StoreWeights(weights);
    private readonly MeleePositioningPlan _melee = new(me);
}
