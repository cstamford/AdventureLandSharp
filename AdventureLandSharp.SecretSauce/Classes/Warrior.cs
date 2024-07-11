using AdventureLandSharp.SecretSauce.Character;
using AdventureLandSharp.SecretSauce.Strategy;
using AdventureLandSharp.SecretSauce.Tactics;
using AdventureLandSharp.Core;
using AdventureLandSharp.Core.SocketApi;
using AdventureLandSharp.Core.Util;
using AdventureLandSharp.Interfaces;
using AdventureLandSharp.Utility;

namespace AdventureLandSharp.SecretSauce.Classes;

public class Warrior(World world, Socket socket, CharacterConfig config) : CharacterBase(world, socket, config) {
    public override CharacterClass Class => CharacterClass.Warrior;

    protected override INode ClassBuild() => new Selector(
        new If(() => ReadyToAgitate, Skill("agitate", Agitate)),
        new If(() => ReadyToStomp && Equipment.MainHand == "basher", Skill("stomp", Stomp)),
        new If(() => ReadyToCleave && Equipment.MainHand == "bataxe", Skill("cleave", Cleave)),
        new If(() => ReadyToWarCry, Skill("warcry", Warcry)),
        new If(() => ReadyToCharge, Skill("charge", Charge)),
        new If(() => ReadyToHardShell, Skill("hardshell", HardShell))
    );

    protected override void OnStrategy() {
        base.OnStrategy();
        //AvailableStrategies.Add(new HighValue_PhoenixScoutStrategy(this));
        //AvailableStrategies.Add(new FarmLocationStrategy(new(World.GetMap("cave"), new(324, -1095))));
        AvailableStrategies.Add(new FarmLocationStrategy(Utils.GetMapLocationForSpawn(World, "squig")));
    }

    protected override CharacterLoadout DesiredLoadout => SelectLoadout();

    private bool ReadyToAgitate_ForProtectingFriends => Enemies.Any(x => x.Distance <= 320 && PartyPlayers.Any(y => y.Player.Name == x.Monster.Target));
    private bool ReadyToAgitate_ForAoEPull => Enemies.Count(x =>
        x.Distance <= 320 &&
        x.PriorityType >= TargetPriorityType.Normal &
        x.Monster.Target != Me.Name &&
        !PartyPlayers.Any(y => y.Player.Name == x.Monster.Target)
    ) >= 3;
    private bool ReadyToAgitate_ForBrawl => Enemies.Any(x => x.Distance <= 320 && (x.Type == "bgoo" || x.Type == "rgoo")); // TODO! Tactic! Wtf.
    private bool ReadyToAgitate_NoBlacklistedMobs => !BlacklistedEnemies.Any(x => x.Distance <= 320);
    private bool ReadyToAgitate_NoBlacklistedEvents => ActiveTactics is not HighValue_PinkGoblinTactics;
    private bool ReadyToAgitate => 
        Me.Mana >= 420 && (
            (ReadyToAgitate_ForAoEPull && Me.Mana >= 1000) || 
            (ReadyToAgitate_ForBrawl && Me.Mana >= 1200) || 
            ReadyToAgitate_ForProtectingFriends ||
            (AttackTarget?.PriorityType >= TargetPriorityType.Priority && AttackTarget?.Monster.Target != Me.Name)
        ) &&
        ReadyToAgitate_NoBlacklistedMobs &&
        ReadyToAgitate_NoBlacklistedEvents;
    private bool ReadyToStomp => Me.Mana >= 120 && InCombat && ActiveTactics switch {
        HighValue_PinkGoblinTactics gobbo => gobbo.ReadyForBurst,
        _ => EnemiesInRange.Count > 0
    };
    private bool ReadyToWarCry => Me.Mana >= 320 && InCombat && ActiveTactics switch {
        HighValue_PinkGoblinTactics gobbo => gobbo.ReadyForBurst,
        _ => EnemiesInRange.Any(x => x.Monster.Health >= Me.DPS * 30)
    };
    private bool ReadyToCharge => EnemiesInRange.Count == 0;
    private bool ReadyToCleave => Me.Mana >= 1000 && 
        Enemies.All(x => 
            x.Distance > 160 ||
            x.PriorityType >= TargetPriorityType.Opportunistic ||
            x.Monster.Target != string.Empty) &&
        EnemiesInRange.Count > 0 &&
        ReadyToAgitate_NoBlacklistedMobs &&
        ReadyToAgitate_NoBlacklistedEvents;
    private bool ReadyToHardShell => false;

    private void Agitate() => Socket.Emit<Outbound.UseSkill>(new("agitate"));
    private void Stomp() => Socket.Emit<Outbound.UseSkill>(new("stomp"));
    private void Cleave() => Socket.Emit<Outbound.UseSkill>(new("cleave"));
    private void Warcry() => Socket.Emit<Outbound.UseSkill>(new("warcry"));
    private void Charge() => Socket.Emit<Outbound.UseSkill>(new("charge"));
    private void HardShell() => Socket.Emit<Outbound.UseSkill>(new("hardshell"));

    private CharacterLoadout SelectLoadout() {
        CharacterLoadout luckSet = default(CharacterLoadout) with { MainHand = "fireblade", OffHand = "mshield", Orb = "rabbitsfoot" };
        CharacterLoadout dpsSet = default(CharacterLoadout) with { MainHand = "fireblade", OffHand = "fireblade", Orb = "orbofstr" };
        CharacterLoadout baseSet = AttackTarget?.PriorityType == TargetPriorityType.Priority && AttackTarget?.Monster.HealthPercent <= 25 ? luckSet : dpsSet;
    
        if (AttackTarget.HasValue) {
            if (ReadyToStomp && Cooldown("stomp").Ready && (Equipment.MainHand == "basher" || Items.Any(x => x.Name == "basher"))) {
                return baseSet with { MainHand = "basher", OffHand = "" };
            }

            if (ReadyToCleave && Cooldown("cleave").Ready && (Equipment.MainHand == "bataxe" || Items.Any(x => x.Name == "bataxe"))) {
                return baseSet with { MainHand = "bataxe", OffHand = "" };
            }
        }

        return baseSet;
    }
}
