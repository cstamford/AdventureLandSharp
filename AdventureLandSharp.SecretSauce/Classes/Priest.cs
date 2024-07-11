using AdventureLandSharp.SecretSauce.Character;
using AdventureLandSharp.SecretSauce.Strategy;
using AdventureLandSharp.Core;
using AdventureLandSharp.Core.SocketApi;
using AdventureLandSharp.Core.Util;
using AdventureLandSharp.Interfaces;
using AdventureLandSharp.Utility;

namespace AdventureLandSharp.SecretSauce.Classes;

public class Priest(World world, Socket socket, CharacterConfig config) : CharacterBase(world, socket, config) {
    public override CharacterClass Class => CharacterClass.Priest;

    protected override INode ActionBuild() => new Selector(
        new If(() => _healTargetName != null, Skill("attack", Heal)),
        base.ActionBuild()
    );

    protected override INode ClassBuild() => new Selector(
        new If(() => 
            AbsorbSinsTarget.HasValue,
            Skill("absorb", AbsorbSins)),
        new If(() => 
            Me.Mana >= 900 && (Me.StatusEffects.WarCry.HasValue || PartyPlayers.Any(x => x.Player.StatusEffects.WarCry.HasValue)),
            Skill("darkblessing", DarkBlessing)),
        new If(() => 
            Me.Mana >= 400 && (Me.HealthPercent <= 50 || PartyPlayers.Any(x => x.Player.HealthPercent <= 50)),
            Skill("partyheal", PartyHeal)),
        new If(() => Me.Mana >= 400 && CurseTarget.HasValue, 
            Skill("curse", Curse))
    );

    protected override void ActionUpdate() {
        _healTargetName = PartyPlayers
            .Select(x => (x.Id, x.Player.HealthPercent, x.Player.HealthMissing))
            .Append((Me.Id, Me.HealthPercent, Me.HealthMissing))
            .Where(x => x.HealthMissing >= Me.AttackDamage*2)
            .OrderBy(x => x.HealthMissing)
            .FirstOrNull()?.Id;

        base.ActionUpdate();
    }

    protected override void OnStrategy() {
        base.OnStrategy();
        AvailableStrategies.Add(new FarmLocationStrategy(new(World.GetMap("cave"), new(324, -1095))));
    }

    private string? _healTargetName;

    private CachedPlayer? AbsorbSinsTarget => PartyPlayers.FirstOrNull(x => Enemies.Any(y => 
        y.Monster.Target == x.Id && (
            x.Player.Id == "Arcanomato" /* TODO: Data driven */ || 
            y.Monster.HealthMissing <= 20000)
        )
    );

    private CachedMonster? CurseTarget => Enemies.FirstOrNull(x => x.PriorityType >= TargetPriorityType.Normal && x.Monster.Health >= Me.AttackDamage);

    private void Heal() => Socket.Emit<Outbound.Heal>(new(_healTargetName!));
    private void AbsorbSins() => Socket.Emit<Outbound.UseSkillOnId>(new("absorb", AbsorbSinsTarget!.Value.Id));
    private void DarkBlessing() => Socket.Emit<Outbound.UseSkill>(new("darkblessing"));
    private void PartyHeal() => Socket.Emit<Outbound.UseSkill>(new("partyheal"));
    private void Curse() => Socket.Emit<Outbound.UseSkillOnId>(new("curse", CurseTarget!.Value.Id));
}
