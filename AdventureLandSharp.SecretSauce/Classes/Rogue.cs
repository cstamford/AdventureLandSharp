using AdventureLandSharp.SecretSauce.Character;
using AdventureLandSharp.SecretSauce.Strategy;
using AdventureLandSharp.SecretSauce.Tactics;
using AdventureLandSharp.Core;
using AdventureLandSharp.Core.SocketApi;
using AdventureLandSharp.Core.Util;
using AdventureLandSharp.Helpers;
using AdventureLandSharp.Interfaces;
using AdventureLandSharp.Utility;

namespace AdventureLandSharp.SecretSauce.Classes;

public class Rogue(World world, Socket socket, CharacterConfig config) : CharacterBase(world, socket, config) {
    public override CharacterClass Class => CharacterClass.Rogue;

    protected override INode ActionBuild() => new Selector(
        new If(() => Me.StatusEffects.Invis.HasValue, new Success()),
        base.ActionBuild()
    );

    protected override INode ClassBuild() => new Selector(
        _cancelInvisCd.IfThenDo(() => ReadyToCancelInvis, CancelInvis),
        new If(() => Me.StatusEffects.Invis.HasValue, new Success()),

        _rogueSwiftnessCd.IfThenDo(() => ReadyToSwiftness, RogueSwiftness),
        new If(() => ReadyToInvis, Skill("invis", Invis)),
        new If(() => ReadyToPoison, Skill("pcoat", Poison)),
        new If(() => ReadyToQuickPunch, Skill("quickpunch", QuickPunch)),
        new If(() => ReadyToMentalBurst, Skill("mentalburst", MentalBurst))
    );

    protected override void OnStrategy() {
        base.OnStrategy();
        AvailableStrategies.Add(new DynamicStrategy(() => UrgentBuffTarget.HasValue, () => UrgentBuffTarget!.Value));
        AvailableStrategies.Add(new FarmLocationStrategy(new(World.GetMap("level2w"), new (-444, 223))));
    }

    protected override void OnTactics() {
        AvailableTactics.Add(new OneEyeAssist(this));
        base.OnTactics();
    }

    private MapLocation? UrgentBuffTarget => CharacterStatuses
        .FirstOrNull(x => 
            !x.Value.StatusEffects.RSpeed.HasValue || 
            x.Value.StatusEffects.RSpeed.Value.Duration <= TimeSpan.FromMinutes(2) ||
            (MyLoc.Equivalent(x.Value.Location, 1000) && x.Value.StatusEffects.RSpeed.Value.Duration <= _rogueSwiftnessRecastHard))
        ?.Value.Location;

    private bool ReadyToCancelInvis => ActiveTactics is Base_TravelTactics && Me.StatusEffects.Invis != null;
    private bool ReadyToInvis => InCombat && Withdrawing;
    private bool ReadyToPoison => 
        Me.Mana >= 800 && 
        Me.Inventory.Items.Any(x => x?.Name == "poison") &&
        ActiveTactics is HighValue_PinkGoblinTactics gobbo && gobbo.ReadyForPoison;
    private bool ReadyToSwiftness => Me.Mana >= 320;
    private bool ReadyToQuickPunch => AttackTarget.HasValue && Me.Mana >= ActiveTactics switch {
        HighValue_PinkGoblinTactics gobbo => gobbo.InKillWindow && Cooldown("pcoat").Ready ? 800 : 500,
        OneEyeAssist oneeye => Me.MaxMana - 500,
        _ => 500,
    };
    private bool ReadyToMentalBurst => AttackTarget.HasValue && Me.Mana >= 400;

    private void Invis() => Socket.Emit<Outbound.UseSkill>(new("invis"));
    private void CancelInvis() => Socket.Emit<Outbound.Stop>(new("invis"));
    private void Poison() {
        Log.Info("Poisoning!");
        Socket.Emit<Outbound.UseSkill>(new("pcoat"));
    }
    private Status RogueSwiftness() {
        if (Me.StatusEffects.RSpeed == null) {
            Socket.Emit<Outbound.UseSkillOnId>(new("rspeed", Me.Id));
            return Status.Success;
        }

        CachedPlayer? target = Players.FirstOrNull(x => 
            !x.Player.StatusEffects.RSpeed.HasValue ||
            x.Player.StatusEffects.RSpeed.Value.Duration <= _rogueSwiftnessRecastSoft);

        if (target != null) {
            Socket.Emit<Outbound.UseSkillOnId>(new("rspeed", target.Value.Player.Id));
            return Status.Success;
        }

        return Status.Fail;
    }
    private void QuickPunch() => Socket.Emit<Outbound.UseSkillOnId>(new("quickpunch", AttackTarget!.Value.Id));
    private void MentalBurst() => Socket.Emit<Outbound.UseSkillOnId>(new("mentalburst", AttackTarget!.Value.Id));

    private readonly Cooldown _cancelInvisCd = new(TimeSpan.FromSeconds(1));
    private readonly Cooldown _rogueSwiftnessCd = new(TimeSpan.FromSeconds(1));
    private readonly TimeSpan _rogueSwiftnessRecastHard = TimeSpan.FromMinutes(5);
    private readonly TimeSpan _rogueSwiftnessRecastSoft = TimeSpan.FromMinutes(40);

}
