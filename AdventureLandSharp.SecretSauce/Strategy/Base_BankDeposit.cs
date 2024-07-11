using AdventureLandSharp.SecretSauce.Character;
using AdventureLandSharp.SecretSauce.Tactics;
using AdventureLandSharp.Core;

namespace AdventureLandSharp.SecretSauce.Strategy;

public class Base_BankDepositStrategy(CharacterBase me) : StrategyBase(me) {
    public override bool Active => 
        !_fullBankSafetySwitch && 
        Items.Count(x => x.Type == ItemType.Bank) >= (Me.Bank?.GetSlotsFreeForMap(MyLoc.Map.Name) > 0 ? 1 : 20);
    public override MapLocation TargetMapLocation => GetBankLocation();
    public override bool Withdrawing => true;
    public override void Update() {
        if (!_fullBankSafetySwitch && Me.Bank?.SlotsFree == 0) {
            Log.Warn($"Bank is full, disabling banking!");
            _fullBankSafetySwitch = true;
        }
    }

    private bool _fullBankSafetySwitch = false;

    private MapLocation GetBankLocation() {
        bool onFloor1ButFull = MyLoc.Map.Name == World.BankLocationFloor1.Map.Name && Me.Bank?.GetSlotsFreeForMap(MyLoc.Map.Name) == 0;
        bool onFloor2 = MyLoc.Map.Name == World.BankLocationFloor2.Map.Name;
        return onFloor2 || onFloor1ButFull ? World.BankLocationFloor2 : World.BankLocationFloor1;
    }
}