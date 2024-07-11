namespace AdventureLandSharp.SecretSauce.Character;

public enum ItemType {
    Bank,
    Destroy,
    Keep,
    Sell
}

public enum TargetPriorityType {
    Blacklist,
    Ignore,
    Opportunistic,
    Normal,
    Priority,
}

public record struct CharacterConfig(
    string PartyLeader,
    float? PartyLeaderFollowDist,
    string? PartyLeaderAssist,

    HashSet<string> DestroyItemsData,
    HashSet<string> KeepItemsData,
    HashSet<string> SellItemsData,

    Dictionary<string, int> TargetsData,

    string? HealthPotion,
    string? ManaPotion,
    string? Elixir,

    string[] TradeTargetsItems,
    string[] TradeTargetsGold,

    bool ShouldLoot,
    bool ShouldUsePassiveRestore,
    bool ShouldHuntPriorityMobs,
    bool ShouldAcceptMagiport,
    bool ShouldDoEvents,

    string[] BlendTargets
) {
    public readonly int GetTargetPriority(string target) => TargetsData.TryGetValue(target, out int priority) ? priority : 0;

    public readonly IEnumerable<string> DestroyItems => DestroyItemsData;
    public readonly IEnumerable<string> KeepItems => KeepItemsData;
    public readonly IEnumerable<string> SellItems => SellItemsData;
    public readonly ItemType GetItemType(string item) {
        if (KeepItemsData.Contains(item) ||
            HealthPotion == item ||
            ManaPotion == item ||
            Elixir == item)
        {
            return ItemType.Keep;
        }

        if (DestroyItemsData.Contains(item)) {
            return ItemType.Destroy;
        }

        if (SellItemsData.Contains(item)) {
            return ItemType.Sell;
        }

        return ItemType.Bank;
    }

    public readonly TargetPriorityType GetTargetPriorityType(string target) => TargetsData.TryGetValue(target, out int priority) ? 
        priority switch {
            >= 25 => TargetPriorityType.Priority,
            >= 5 => TargetPriorityType.Normal,
            >= 1 => TargetPriorityType.Opportunistic,
            <= -1 => TargetPriorityType.Blacklist,
            _ => TargetPriorityType.Ignore
        } : TargetPriorityType.Ignore;
}
