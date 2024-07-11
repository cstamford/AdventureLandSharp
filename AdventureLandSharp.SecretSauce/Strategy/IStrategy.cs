using AdventureLandSharp.SecretSauce.Character;
using AdventureLandSharp.Core;
using AdventureLandSharp.Core.SocketApi;
using AdventureLandSharp.Core.Util;

namespace AdventureLandSharp.SecretSauce.Tactics;

public interface IStrategy {
    public abstract bool Active { get; }
    public abstract MapLocation TargetMapLocation { get; }
    public abstract bool Withdrawing { get; }
    public void Update();
}

public abstract class StrategyBase(CharacterBase me) : IStrategy {
    public abstract bool Active { get; }
    public abstract MapLocation TargetMapLocation { get; }
    public abstract bool Withdrawing { get; }
    public virtual void Update() { }

    protected MapLocation MyLoc => me.EntityLocation;
    protected MapGridCell MyLocGrid => MyLoc.Grid();
    protected LocalPlayer Me => me.Entity;
    protected CharacterBase MyChar => me;
    protected CharacterConfig Cfg => me.Cfg;
    protected Logger Log => _log;
    protected Socket Socket => me.Socket;
    protected World World => me.World;
    protected CachedEquipment Equipment => me.Equipment;
    protected IReadOnlyList<CachedEntity> Entities => me.Entities;
    protected IReadOnlyList<CachedPlayer> Players => me.Players;
    protected IReadOnlyList<CachedPlayer> PartyPlayers => me.PartyPlayers;
    protected IReadOnlyList<CachedMonster> Enemies => me.Enemies;
    protected IReadOnlyList<CachedMonster> EnemiesInRange => me.EnemiesInRange;
    protected IReadOnlyList<CachedMonster> EnemiesTargetingUs => me.EnemiesTargetingUs;
    protected IReadOnlyList<CachedMonster> EnemiesNotTargetingUs => me.EnemiesNotTargetingUs;
    protected IReadOnlyList<CachedMonster> BlacklistedEnemies => me.BlacklistedEnemies;
    protected IReadOnlyList<CachedNpc> Npcs => me.Npcs;
    protected IReadOnlyList<CachedItem> Items => me.Items;
    protected IReadOnlyDictionary<string, MapLocation> LastSightedNpcs => me.LastSightedNpcs;
    protected SessionEventBusHandle EventBusHandle => me.EventBusHandle;
    protected IReadOnlyList<MapLocation> EventMapLocations => me.EventMapLocations;
    protected PriorityMobSpottedEvent? PriorityMobHuntEvent => me.PriorityMobHuntEvent;

    private readonly Logger _log = new(me.Entity.Name, "Strategy");
}