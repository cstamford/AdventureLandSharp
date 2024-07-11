using AdventureLandSharp.SecretSauce.Strategy;
using AdventureLandSharp.SecretSauce.Tactics;
using AdventureLandSharp.Core;

namespace AdventureLandSharp.SecretSauce.Character;

public abstract partial class CharacterBase {
    public IStrategy ActiveStrategy => AvailableStrategies.First(x => x.Active);

    public MapLocation TargetMapLocation => ActiveStrategy.TargetMapLocation;
    public virtual bool Withdrawing => ActiveStrategy.Withdrawing && !Enemies.Any(x => x.PriorityType == TargetPriorityType.Priority);

    protected List<IStrategy> AvailableStrategies => _strategies;

    protected virtual void OnStrategy() {
        _strategies.Add(new Base_BankDepositStrategy(this));
        _strategies.Add(new Base_BuySellStrategy(this));
        _strategies.Add(new Base_EventsStrategy(this));
        _strategies.Add(new Base_PriorityMobsStrategy(this));
        _strategies.Add(new Base_BlendStrategy(this));
    }

    protected virtual void StrategyUpdate() {
        ActiveStrategy.Update();
    }

    private List<IStrategy> _strategies = [];
}
