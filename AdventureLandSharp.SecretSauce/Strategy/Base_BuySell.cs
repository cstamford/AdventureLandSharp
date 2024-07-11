using AdventureLandSharp.SecretSauce.Character;
using AdventureLandSharp.SecretSauce.Tactics;
using AdventureLandSharp.Core;

namespace AdventureLandSharp.SecretSauce.Strategy;

public class Base_BuySellStrategy(CharacterBase me) : StrategyBase(me) {
    public override bool Active => 
        (Cfg.HealthPotion != null && (CharacterBase.PotTarget - Items.Where(x => x.Name == Cfg.HealthPotion).Sum(x => x.Quantity) > CharacterBase.PotThreshold)) ||
        (Cfg.ManaPotion != null && (CharacterBase.PotTarget - Items.Where(x => x.Name == Cfg.ManaPotion).Sum(x => x.Quantity) > CharacterBase.PotThreshold)) ||
        (Items.Count >= 35 && Items.Any(x => x.Type == ItemType.Sell));
    public override MapLocation TargetMapLocation => CharacterBase.BuySellLocation(MyLoc.Map.Name, World.PotionLocations);
    public override bool Withdrawing => true;
}
