using AdventureLandSharp.SecretSauce.Character;
using AdventureLandSharp.SecretSauce.Tactics;
using AdventureLandSharp.Core;
using AdventureLandSharp.Core.SocketApi;

namespace AdventureLandSharp.SecretSauce.Strategy;

public class Base_BlendStrategy(CharacterBase me) : StrategyBase(me) {
    public override bool Active => 
        Cfg.BlendTargets.Length != 0 &&
        Cfg.BlendTargets.All(x => x != Me.Skin) && 
        Entities.Any(x => x.Entity is Monster && Cfg.BlendTargets.Any(y => y == x.Entity.Type));
    public override MapLocation TargetMapLocation => MyLoc with {
        Position = Entities.First(x => Cfg.BlendTargets.Any(y => y == x.Entity.Type)).Position
    };
    public override bool Withdrawing => true;
}
