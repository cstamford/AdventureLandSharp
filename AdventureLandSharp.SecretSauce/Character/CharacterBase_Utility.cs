using System.Diagnostics;
using AdventureLandSharp.Core;
using AdventureLandSharp.Core.SocketApi;
using AdventureLandSharp.Core.Util;
using AdventureLandSharp.Helpers;
using AdventureLandSharp.Utility;

namespace AdventureLandSharp.SecretSauce.Character;

public abstract partial class CharacterBase {
    public const int PotThreshold = 8000;
    public const int PotTarget = 9999;
    public static MapLocation BuySellLocation(string mapName, MapLocation[] candidates) => mapName switch {
        "winterland" or "winter_cave" or "winter_inn" => candidates.First(x => x.Map.Name == "winter_inn"),
        "halloween" => candidates.First(x => x.Map.Name == "halloween"),
        _ => candidates.First(x => x.Map.Name == "main")
    };

    protected virtual INode UtilityBuild() => new Selector(
        _lootCd.IfThenDo(() => CanLoot, LootItems),
        new If(() => IsTeleporting, new Success()),

        _blendCd.IfThenDo(() => CanBlend, Blend),

        _equipCd.ThenDo(HandleEquipSwap),
        _sellCd.IfThenDo(() => CanSell, SellItems),
        _bankCd.IfThenDo(() => CanBank, BankItems),

        _tradeGoldCd.IfThenDo(() => CanTradeGold && NeedsToTradeGold, TradeGold),
        _tradeItemsCd.IfThenDo(() => CanTradeItems && NeedsToTradeItems, TradeItems),

        _buyCd.IfThenDo(() => CanBuy, new Selector(
            new If(() => HpPotionsToBuy > 0, () => BuyItem(Cfg.HealthPotion!, HpPotionsToBuy)),
            new If(() => MpPotionsToBuy > 0, () => BuyItem(Cfg.ManaPotion!, MpPotionsToBuy))
        ))
    );

    protected virtual void UtilityUpdate() {
        _utilityBt.Tick();
    }

    protected virtual CharacterLoadout DesiredLoadout => default;

    private const int _keepGold = 10000000;
    private const int _keepGoldBuffer = 1000000;

    private readonly INode _utilityBt;

    private readonly Cooldown _blendCd = new(NetworkThrottleReadResponse);
    private readonly Cooldown _equipCd = new(TimeSpan.FromMilliseconds(50));
    private readonly Cooldown _buyCd = new(NetworkThrottleReadResponse);
    private readonly Cooldown _sellCd = new(NetworkThrottleReadResponse * 2);
    private readonly Cooldown _bankCd = new(NetworkThrottleReadResponse * 4);
    private readonly Cooldown _lootCd = new(NetworkThrottleReadResponse);
    private readonly Cooldown _tradeGoldCd = new(NetworkThrottleReadResponse);
    private readonly Cooldown _tradeItemsCd = new(NetworkThrottleReadResponse * 4);

    private bool NeedsToTradeGold => GoldToTrade > 0;
    private bool NeedsToTradeItems => Items.Any(x => x.Type != ItemType.Keep);

    private bool CanBlend => Cfg.BlendTargets.All(x => x != Me.Skin) && Cfg.BlendTargets.Any(x => x == Entities.OrderBy(x => x.Distance).FirstOrNull(x => x.Entity is Monster)?.Entity.Type);
    private bool CanBuy => Me.CodeCallCost <= 150 && MyLoc.Equivalent(BuySellLocation(MyLoc.Map.Name, World.PotionLocations), GameConstants.BuyDist);
    private bool CanSell => Me.CodeCallCost <= 150 && MyLoc.Equivalent(BuySellLocation(MyLoc.Map.Name, World.PotionLocations), GameConstants.SellDist);
    private bool CanBank => Me.CodeCallCost <= 100 && Me.Bank?.GetSlotsFreeForMap(MyLoc.Map.Name) > 0;
    private bool CanLoot => Me.CodeCallCost <= 150 && Cfg.ShouldLoot && Items.Count < 42 && Drops.Any();
    private bool CanTradeGold => Me.CodeCallCost <= 150 && TradeGoldTarget.HasValue;
    private bool CanTradeItems => Me.CodeCallCost <= 150 && TradeItemsTarget.HasValue;

    private int HpPotionsToBuy =>
        Cfg.HealthPotion != null ? PotTarget - Items.Where(x => x.Name == Cfg.HealthPotion).Sum(x => x.Quantity) : 0;
    private int MpPotionsToBuy =>
        Cfg.ManaPotion != null ? PotTarget - Items.Where(x => x.Name == Cfg.ManaPotion).Sum(x => x.Quantity) : 0;
    private IEnumerable<DropData> Drops =>
        Socket.Drops.Where(x => x.Position.SimpleDist(Me.Position) <= GameConstants.LootDist);
    private int GoldToTrade => 
        (int)Me.Inventory.Gold - _keepGold - _keepGoldBuffer;
    private CachedPlayer? TradeGoldTarget => 
        PartyPlayers.FirstOrNull(x => Cfg.TradeTargetsGold.Any(y => y == x.Name) && x.Position.SimpleDist(Me.Position) < GameConstants.TradeDist);
    private CachedPlayer? TradeItemsTarget =>
        PartyPlayers.FirstOrNull(x => Cfg.TradeTargetsItems.Any(y => y == x.Name) && x.Position.SimpleDist(Me.Position) < GameConstants.TradeDist);

    private Status HandleEquipSwap() {
        if (Me.CodeCallCost >= 150) {
            return Status.Fail;
        }

        CharacterLoadout desiredLoadout = DesiredLoadout;

        List<string> unequips = [];
        List<(string, int)> equips = [];
        HashSet<int> equipped = [];

        HandleEquip("ring1", Equipment.Ring1, desiredLoadout.Ring1);
        HandleEquip("ring2", Equipment.Ring2, desiredLoadout.Ring2);
        HandleEquip("earring1", Equipment.Earring1, desiredLoadout.Earring1);
        HandleEquip("earring2", Equipment.Earring2, desiredLoadout.Earring2);
        HandleEquip("belt", Equipment.Belt, desiredLoadout.Belt);
        HandleEquip("mainhand", Equipment.MainHand, desiredLoadout.MainHand);
        HandleEquip("offhand", Equipment.OffHand, desiredLoadout.OffHand);
        HandleEquip("helmet", Equipment.Helmet, desiredLoadout.Helmet);
        HandleEquip("chest", Equipment.Chest, desiredLoadout.Chest);
        HandleEquip("pants", Equipment.Pants, desiredLoadout.Pants);
        HandleEquip("shoes", Equipment.Shoes, desiredLoadout.Shoes);
        HandleEquip("gloves", Equipment.Gloves, desiredLoadout.Gloves);
        HandleEquip("amulet", Equipment.Amulet, desiredLoadout.Amulet);
        HandleEquip("orb", Equipment.Orb, desiredLoadout.Orb);
        HandleEquip("cape", Equipment.Cape, desiredLoadout.Cape);

        void HandleEquip(string slotName, string current, string? desired) {
            if (desired == null || current == desired) {
                return;
            }

            if (desired == string.Empty) {
                unequips.Add(slotName);
                return; 
            }

            int idx = Me.Inventory.Items
                .Select((item, idx) => (Idx: idx, Item: item))
                .Where(x => !equipped.Contains(x.Idx) && x.Item != null && x.Item.Value.Name == desired)
                .OrderByDescending(x => x.Item!.Value.Level)
                .Select(x => x.Idx)
                .FirstOrDefault(-1);

            if (idx != -1) {
                equips.Add(new(slotName, idx));
                equipped.Add(idx);
                return;
            }
        }

        foreach (string slotName in unequips) {
            Log.Debug($"Unequipping {slotName}");
            Socket.Emit<Outbound.UnequipSlot>(new(slotName));
        }

        foreach ((string slot, int idx) in equips) {
            Log.Debug($"Equipping {idx} to {slot}");
            Socket.Emit<Outbound.EquipSlot>(new(slot, idx));
        }

        return unequips.Count > 0 || equips.Count > 0 ? Status.Success : Status.Fail;
    }

    private Status BankItems() {
        Debug.Assert(CanBank);

        int deposited = 0;
        foreach (CachedItem item in Items.Where(x => x.Type == ItemType.Bank)) {
            int depositIdx = BankItems_GetDepositTabIdx(item);
            if (depositIdx != -1) {
                Socket.Emit<Outbound.BankDeposit>(new(item.Slot, depositIdx));
                Log.Debug($"Depositing {item.Name} in slot {item.Slot} to tab {depositIdx}");
                if (++deposited >= 5) {
                    break;
                }
            }
        }

        return Status.Success;
    }

    private int BankItems_GetDepositTabIdx(CachedItem item) {
        IEnumerable<(int Index, Item?[] Tab)> tabs = Me.Bank?.GetValidTabsForMap(MyLoc.Map.Name) ?? [];

        foreach ((int idx, Item?[] tab) in tabs) {
            Item? stackable = tab.FirstOrDefault(x => x?.Name == item.Name && x?.Quantity >= 2);

            if (stackable != null && stackable.Value.Quantity + item.Quantity <= 9999) {
                return idx;
            }
        }

        foreach ((int idx, Item?[] tab) in tabs.OrderByDescending(x => PlayerBank.GetSlotsFreeInTab(x.Tab))) {
            if (PlayerBank.GetSlotsFreeInTab(tab) > 0) {
                return idx;
            }
        }

        return -1;
    }

    private void Blend() => Socket.Emit<Outbound.Blend>(new());

    private Status BuyItem(string item, int count) {
        Debug.Assert(CanBuy);

        int cost = (int)(count * World.Data.Items[item].Gold);
        if (Me.Inventory.Gold >= cost) {
            Socket.Emit<Outbound.Buy>(new(item, count));
            Log.Debug($"Buying {item}x{count}");
            return Status.Success;
        }

        return Status.Fail;
    }

    private Status LootItems() {
        Debug.Assert(CanLoot);

        bool lootedAny = false;
        foreach (DropData drop in Drops) {
            Socket.Emit<Outbound.OpenChest>(new(drop.Id));
            Log.Debug($"Looting {drop.Id}");
            lootedAny = true;
        }

        return lootedAny ? Status.Success : Status.Fail;
    }

    private Status SellItems() {
        Debug.Assert(CanSell);

        int sold = 0;
        foreach (CachedItem item in Items.Where(x => x.Type == ItemType.Sell)) {
            Socket.Emit<Outbound.SellItem>(new(item.Slot, item.Quantity));
            Log.Debug($"Selling {item.Name} in slot {item.Slot}");
            if (++sold >= 5) {
                break;
            }
        }

        return sold > 0 ? Status.Success : Status.Fail;
    }

    private Status TradeGold() {
        Debug.Assert(CanTradeGold);

        int goldToTrade = GoldToTrade;
        CachedPlayer target = TradeGoldTarget!.Value;
        Socket.Emit<Outbound.SendGold>(new(target.Name, goldToTrade));    
        Log.Debug($"Trading {goldToTrade} gold to {target}");

        return goldToTrade > 0 ? Status.Success : Status.Fail;
    }

    private Status TradeItems() {
        Debug.Assert(CanTradeItems);

        int traded = 0;
        foreach (CachedItem item in Items.Where(x => x.Type != ItemType.Keep)) {
            CachedPlayer target = TradeItemsTarget!.Value;
            Socket.Emit<Outbound.SendItem>(new(target.Name, item.Slot, item.Quantity));
            Log.Debug($"Trading {item.Name} in slot {item.Slot} to {target}");
            if (++traded >= 5) {
                break;
            }
        }

        return traded > 0 ? Status.Success : Status.Fail;
    }
}

public record struct CharacterLoadout(
    string? Ring1,
    string? Ring2,
    string? Earring1,
    string? Earring2,
    string? Belt,
    string? MainHand,
    string? OffHand,
    string? Helmet,
    string? Chest,
    string? Pants,
    string? Shoes,
    string? Gloves,
    string? Amulet,
    string? Orb,
    string? Cape
);
