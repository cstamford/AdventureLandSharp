using System.Diagnostics;
using AdventureLandSharp.SecretSauce.Character;
using AdventureLandSharp.Core.SocketApi;
using AdventureLandSharp.Core.Util;

namespace AdventureLandSharp.SecretSauce.Tactics;

public class HighValue_PinkGoblinTactics : TacticsBase {
    public HighValue_PinkGoblinTactics(CharacterBase character) : base(character) { 
        Socket.OnHit += evt => {
            if (evt.TargetId == AttackTarget?.Id) {
                _dps_7.Add(evt.Damage);
            }
        };
    }

    public override bool Active =>
        !MyChar.Withdrawing &&
        Cfg.GetTargetPriorityType("pinkgoblin") == TargetPriorityType.Priority &&
        AttackTarget.HasValue;

    public override CachedMonster? AttackTarget => PinkGoblins
        .Where(x => Cfg.PartyLeaderAssist == null || x.Monster.Target == Cfg.PartyLeaderAssist)
        .OrderByDescending(x => Entities.Count(y => y.Entity.Target == x.Monster.Id))
        .FirstOrNull();

    public override IPositioningPlan PositioningPlan => new MeleePositioningPlan(MyChar);

    public override void Update() {
        string mobId = AttackTarget!.Value.Monster.Id;

        if (mobId != _mobId) {
            _mobId = mobId;
            _dps_7.Clear();
            _isKilling = false;
            _isKillingNeedsPoison = false;
        }

        _isKilling |= _dps_7.Sum * 1.35 >= AttackTarget!.Value.Monster.Health;
        _isKillingNeedsPoison |= _isKilling && TimeUntilNextHeal.TotalSeconds is <= 1.5 and >= 0.5;

        bool anyTargettingMe = PinkGoblins.Any(x => x.Monster.Target == Me.Name);

        if (anyTargettingMe) {
            _targetedBailTime ??= DateTimeOffset.UtcNow.AddSeconds(2);
        } else {
            _targetedBailTime = null;
        }

        if (_targetedBailTime.HasValue && DateTimeOffset.UtcNow >= _targetedBailTime) {
            Log.Warn("Forcing jail as we are being targetting by pinkgoblin!");
            MyChar.ForceJail();
        }
    }

    public TimeSpan TimeUntilNextHeal => CalculateTimeUntilNextHeal();
    public TimeSpan TimeUntilStunFinished => AttackTarget!.Value.Monster.StatusEffects.Stunned?.Duration ?? TimeSpan.Zero;
    public bool InKillWindow => _isKilling;

    public bool ReadyForBurst => InKillWindow && TimeUntilNextHeal.TotalSeconds is <= 1.5 and >= 0.5;
    public bool ReadyForPoison => _isKillingNeedsPoison;

    private IEnumerable<CachedMonster> PinkGoblins => Enemies.Where(x => x.Type == "pinkgoblin");

    private readonly MovingAverage<double> _dps_7 = new(TimeSpan.FromSeconds(7));
    private string? _mobId = null;
    private DateTimeOffset? _targetedBailTime;
    private bool _isKilling = false;
    private bool _isKillingNeedsPoison = false;

    private TimeSpan CalculateTimeUntilNextHeal() {
        Debug.Assert(AttackTarget.HasValue);
        CachedMonster target = AttackTarget.Value;
        StatusEffect selfHealing = target.Monster.StatusEffects.SelfHealing!.Value;
        StatusEffect? stunned = target.Monster.StatusEffects.Stunned;
        return stunned.HasValue && stunned.Value.Duration > selfHealing.Duration ? stunned.Value.Duration : selfHealing.Duration;
    }
}
