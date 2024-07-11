using AdventureLandSharp.SecretSauce.Character;
using AdventureLandSharp.SecretSauce.Strategy;
using AdventureLandSharp.Core;
using AdventureLandSharp.Core.SocketApi;
using AdventureLandSharp.Core.Util;
using AdventureLandSharp.Helpers;
using AdventureLandSharp.Interfaces;
using AdventureLandSharp.Utility;

namespace AdventureLandSharp.SecretSauce.Classes;

public class Ranger(World world, Socket socket, CharacterConfig config) : CharacterBase(world, socket, config) {
    public override CharacterClass Class => CharacterClass.Ranger;

    protected override INode ActionBuild() => new Selector(
        new If(() => ReadyToFiveShot, Skill("attack", AttackFiveShot)),
        new If(() => ReadyToThreeShot, Skill("attack", AttackThreeShot)),
        base.ActionBuild()
    );

    protected override INode ClassBuild() => new Selector(
        _confettiCd.IfThenDo(() => ReadyToConfetti, Confetti)
    );

    protected override void ActionUpdate() {
        _aoeTargets.Clear();

        foreach (CachedMonster enemy in EnemiesInRange) {
            if (enemy.Priority != AttackTarget?.Priority) {
                continue;
            }

            if (enemy.Monster.Target == string.Empty && enemy.Monster.Health >= Me.AttackDamage) {
                continue;
            }

            if (Players.Any(x => !PartyPlayers.Any(y => y.Id == x.Id) && enemy.Id == x.Player.Target)) {
                continue;
            }

            _aoeTargets.Add(enemy);
        }

        base.ActionUpdate();
    }

    protected override void ClassUpdate() {
        if (Kane.HasValue) {
            _lastSawKane = DateTimeOffset.UtcNow;
        }

        base.ClassUpdate();
    }

    protected override void OnStrategy() {
        base.OnStrategy();
        AvailableStrategies.Add(new FarmLocationStrategy(Utils.GetMapLocationForSpawn(World, "crab")));
    }

    private bool ReadyToConfetti => Items.Any(x => x.Name == "confetti") && 
        DateTimeOffset.UtcNow.Subtract(_lastSawKane) <= TimeSpan.FromMinutes(2) &&
        (!Kane.HasValue || Kane.Value.Position.SimpleDist(TargetMapLocation.Position) >= 300);
    private bool ReadyToFiveShot => Me.Mana >= 520 && Me.Level >= 75 && _aoeTargets.Count >= 5;
    private bool ReadyToThreeShot => Me.Mana >= 400 && Me.Level >= 60 && _aoeTargets.Count >= 3;

    private void Confetti() => Socket.Emit<Outbound.Throw>(new(Items.First(x => x.Name == "confetti").Slot));
    private void AttackFiveShot() => Socket.Emit<Outbound.UseSkillOnIds>(new("5shot", [..GetBestTargetIds(5)]));
    private void AttackThreeShot() => Socket.Emit<Outbound.UseSkillOnIds>(new("3shot", [..GetBestTargetIds(3)]));

    private readonly Cooldown _confettiCd = new(TimeSpan.FromSeconds(20));

    private readonly List<CachedMonster> _aoeTargets = [];

    private DateTimeOffset _lastSawKane = DateTimeOffset.MinValue;
    private CachedNpc? Kane => Npcs.FirstOrNull(x => x.Name == "Kane");

    private IEnumerable<string> GetBestTargetIds(int count) => _aoeTargets
        .OrderByDescending(x => x.Distance)
        .Take(count)
        .Select(x => x.Id);
}
