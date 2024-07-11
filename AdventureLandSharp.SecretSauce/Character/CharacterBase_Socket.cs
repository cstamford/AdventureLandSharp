using System.Numerics;
using System.Text.Json;
using AdventureLandSharp.Core;
using AdventureLandSharp.Core.SocketApi;
using AdventureLandSharp.Core.Util;
using AdventureLandSharp.Helpers;

namespace AdventureLandSharp.SecretSauce.Character;

public abstract partial class CharacterBase {
    public CachedEquipment Equipment { get; private set; }
    public IReadOnlyList<CachedEventMonster> EventMonsters => _eventMonsters;
    public IReadOnlyList<CachedEntity> Entities => _entities;
    public IReadOnlyList<CachedPlayer> Players => _players;
    public IReadOnlyList<CachedPlayer> PartyPlayers => _partyPlayers;
    public IReadOnlyList<CachedMonster> Enemies => _enemies;
    public IReadOnlyList<CachedMonster> EnemiesInRange => _enemiesInRange;
    public IReadOnlyList<CachedMonster> EnemiesTargetingUs => _enemiesTargetingUs;
    public IReadOnlyList<CachedMonster> EnemiesNotTargetingUs => _enemiesNotTargetingUs;
    public IReadOnlyList<CachedMonster> BlacklistedEnemies => _blacklistedEnemies;
    public IReadOnlyList<CachedNpc> Npcs => _npcs;
    public IReadOnlyList<CachedItem> Items => _items;
    public IReadOnlyDictionary<string, MapLocation> LastSightedNpcs => _lastSightedNpcs;

    protected MapLocation MyLoc { get; private set; }
    protected MapLocation MyLocLast { get; private set; }

    protected virtual void OnSocket() {
        Socket.OnCorrection += OnSocket;
        Socket.OnGameResponse += OnSocket_GameResponse;
        Socket.OnMagiportRequest += OnSocket;
        Socket.OnNewMap += OnSocket;
        Socket.OnServerInfo += OnSocket;
        Socket.OnSkillTimeout += OnSocket;
    }

    protected void SocketUpdate() {
        MyLocLast = MyLoc == default ? new(World.GetMap(Me.MapName), Me.Position) : MyLoc;
        MyLoc = new(World.GetMap(Me.MapName), Me.Position);

        if (MyLoc != MyLocLast || EnemiesTargetingUs.Count > 0) {
            _lastPositionChangeTime = DateTimeOffset.UtcNow;
        }

        Equipment = new(Socket.Player.Equipment);

        _entities.Clear();
        _players.Clear();
        _partyPlayers.Clear();
        _enemies.Clear();
        _enemiesInRange.Clear();
        _enemiesTargetingUs.Clear();
        _enemiesNotTargetingUs.Clear();
        _blacklistedEnemies.Clear();
        _npcs.Clear();
        _items.Clear();

        foreach (Entity x in Socket.Entities) {
            _entities.Add(new(Entity: x, Distance: Me.Dist(x)));
        }

        _entities.Sort((x, y) => x.Distance.CompareTo(y.Distance));

        CachedEntity? assist = _entities.FirstOrNull(x => x.Entity is Player p && p.Name == Cfg.PartyLeaderAssist);
        CachedEntity? assistTarget = _entities.FirstOrNull(x => x.Entity is Monster m && m.Id == assist?.Entity.Target);

        if (assistTarget.HasValue) {
            _lastValidAssistTarget = assistTarget.Value.Entity.Id;
        }

        foreach (CachedEntity e in _entities) {
            if (e.Entity is Monster m) {
                int priority = Cfg.GetTargetPriority(m.Type);
                TargetPriorityType priorityType = Cfg.GetTargetPriorityType(m.Type);

                if (m.MaxHealth <= Me.DPS*2 && priorityType == TargetPriorityType.Ignore) {
                    priorityType = TargetPriorityType.Opportunistic;
                }

                if (Cfg.PartyLeaderAssist != null && priorityType <= TargetPriorityType.Normal) {
                    priorityType = _lastValidAssistTarget == m.Id ? TargetPriorityType.Normal : TargetPriorityType.Ignore;
                }

                if (priorityType < TargetPriorityType.Priority && m.Dead) {
                    priorityType = TargetPriorityType.Ignore;
                }

                if (priorityType >= TargetPriorityType.Ignore) {
                    _enemies.Add(new(m, e.Distance, priorityType, priority));
                } else {
                    _blacklistedEnemies.Add(new(m, e.Distance, priorityType, priority));
                }
            } else if (e.Entity is Player p) {
                _players.Add(new(p, e.Distance));
                if (EventBusHandle.Participants.Contains(p.Name)) {
                    _partyPlayers.Add(new(p, e.Distance));
                }
            } else if (e.Entity is Npc n) {
                _npcs.Add(new(n, e.Distance));
            }
        }

        _enemies.Sort((x, y) => {
            int priorityType = y.PriorityType.CompareTo(x.PriorityType);
            if (priorityType != 0) return priorityType;

            int priority = y.Priority.CompareTo(x.Priority);
            if (priority != 0) return priority;

            return x.Distance.CompareTo(y.Distance);
        });

        foreach (CachedMonster m in _enemies) {
            if (m.Distance < Me.AttackRange) {
                _enemiesInRange.Add(m);
            }

            if (m.Monster.Target == Me.Name) {
                _enemiesTargetingUs.Add(m);
            } else {
                _enemiesNotTargetingUs.Add(m);
            }
        }

        for (int i = 0; i < Me.Inventory.Items.Count; ++i) {
            Item? item = Me.Inventory.Items[i];
            if (item != null) {
                ItemType itemType = Cfg.GetItemType(item.Value.Name);

                if (item.Value.Level > 0) {
                    itemType = ItemType.Keep;
                } else if (item.Value.SpecialType != null) {
                    itemType = ItemType.Bank;
                }

                _items.Add(new(item.Value, itemType, i));
            }
        }
    }

    private List<CachedEventMonster> _eventMonsters = [];
    private List<(string JoinName, MapLocation JoinDest)> _eventJoins = [];
    private List<MapLocation> _eventMapLocations = [];
    private List<CachedEntity> _entities = [];
    private List<CachedPlayer> _players = [];
    private List<CachedPlayer> _partyPlayers = [];
    private List<CachedMonster> _enemies = [];
    private List<CachedMonster> _enemiesInRange = [];
    private List<CachedMonster> _enemiesTargetingUs = [];
    private List<CachedMonster> _enemiesNotTargetingUs = [];
    private List<CachedMonster> _blacklistedEnemies = [];
    private List<CachedNpc> _npcs = [];
    private List<CachedItem> _items = [];
    private Dictionary<string, MapLocation> _lastSightedNpcs = [];

    private string? _lastValidAssistTarget = null;

    private void OnSocket(Inbound.CorrectionData evt) {
        ResetMovement();
    }

    private void OnSocket(Inbound.MagiportRequestData evt) {
        if (Cfg.ShouldAcceptMagiport && EventBusHandle.Participants.Contains(evt.Name)) {
            Socket.Emit<Outbound.MagiportResponseData>(new(evt.Name));
        }
    }

    private void OnSocket(Inbound.NewMapData evt) {
        if (Movement?.CurrentEdge?.Dest.Map.Name != evt.MapName) {
            ResetMovement();
        }
    }

    private void OnSocket(Inbound.ServerInfo evt) {
        if (!Cfg.ShouldDoEvents) {
            return;
        }

        _eventMonsters.Clear();
        _eventJoins.Clear();
        _eventMapLocations.Clear();

        if (Me.StatusEffects.HopSickness.HasValue) {
            return;
        }

        foreach ((Inbound.ServerInfo_EventMonsterType monsterType, Inbound.ServerInfo_EventMonster? data) in evt.EventMonsters) {
            if (!data.HasValue || !data.Value.IsLive) {
                continue;
            }

            MapLocation location = new(World.GetMap(data.Value.MapName), new(data.Value.MapX, data.Value.MapY));
            _eventMonsters.Add(new(monsterType, data.Value, location));
        }

        foreach (CachedEventMonster m in _eventMonsters
            .Where(x => Cfg.GetTargetPriorityType(x.Type) > TargetPriorityType.Ignore)
            .OrderBy(x => x.Data.Health))
        {
            _eventMapLocations.Add(m.Location);

            if (m.MonsterType == Inbound.ServerInfo_EventMonsterType.BigAssCrab && !MyLoc.Equivalent(World.BigAssCrabLocation, 256)) {
                _eventJoins.Add(new(GameConstants.BigAssCrabJoinName, World.BigAssCrabLocation));
            }

            if (m.MonsterType == Inbound.ServerInfo_EventMonsterType.Franky && !MyLoc.Equivalent(World.FrankyLocation, 256)) {
                _eventJoins.Add(new(GameConstants.FrankyJoinName, World.FrankyLocation));
            }

            if (m.MonsterType == Inbound.ServerInfo_EventMonsterType.IceGolem && !MyLoc.Equivalent(World.IceGolemLocation, 256)) {
                _eventJoins.Add(new(GameConstants.IceGolemJoinName, World.IceGolemLocation));
            }
        }

        if (evt.GooBrawl.HasValue) {
            _eventJoins.Add(new(GameConstants.GooBrawlJoinName, World.GooBrawlLocation));
            _eventMapLocations.Add(World.GooBrawlLocation);
        }

        Log.Debug($"Updated server info received. Monsters: {_eventMonsters.Count}, Joins: {_eventJoins.Count}, Map locations: {_eventMapLocations.Count}.");

        if (_eventMonsters.Count > 0) {
            Log.Debug($"Event monsters: {string.Join(", ", _eventMonsters.Select(x => x.Type))}");
        }

        if (_eventJoins.Count > 0) {
            Log.Debug($"Event joins: {string.Join(", ", _eventJoins)}");
        }

        if (_eventMapLocations.Count > 0) {
            Log.Debug($"Event map locations: {string.Join(", ", _eventMapLocations)}");
        }
    }

    private void OnSocket(Inbound.SkillTimeoutData evt) {
        string skillName = evt.SkillName;
        string skillCdName = (World.Data.Skills.TryGetValue(evt.SkillName, out GameDataSkillDef def) ? def.CdShareName : null) ?? skillName;

        Cooldown cd = Cooldown(skillCdName, TimeSpan.FromMilliseconds(evt.MillisecondsUntilReady) - Socket.Latency);
        cd.Restart();
    }

    private void OnSocket_GameResponse(JsonElement evt) {
        if (evt.TryGetProperty("skill", out JsonElement skill) && evt.TryGetProperty("ms", out JsonElement ms)) {
            OnSocket(new Inbound.SkillTimeoutData(skill.GetString()!, ms.GetInt32()));
        }
    }
}

public readonly record struct CachedEntity(Entity Entity, float Distance) {
    public override string ToString() => $"<{Distance:f2}> {Entity}";

    public string Name => Entity.Name;
    public string Id => Entity.Id;
    public Vector2 Position => Entity.Position;
}

public readonly record struct CachedPlayer(Player Player, float Distance) {
    public override string ToString() => $"<{Distance:f2}> {Player}";

    public string Name => Player.Name;
    public string Id => Player.Id;
    public Vector2 Position => Player.Position;
}

public readonly record struct CachedMonster(Monster Monster, float Distance, TargetPriorityType PriorityType, int Priority) {
    public override string ToString() => $"{PriorityType}:{Priority} <{Distance:f2}> {Monster}";

    public string Name => Monster.Name;
    public string Id => Monster.Id;
    public string Type => Monster.Type;
    public Vector2 Position => Monster.Position;
}

public readonly record struct CachedNpc(Npc Npc, float Distance) {
    public override string ToString() => $"<{Distance:f2}> {Npc}";

    public string Name => Npc.Name;
    public string Id => Npc.Id;
    public Vector2 Position => Npc.Position;

    public readonly bool InAuraRange(Vector2 position) => Npc.NpcType switch {
        "citizen0" => position.SimpleDist(Npc.Position) <= GameConstants.AuraDist,
        _ => false
    };
}

public readonly record struct CachedItem(Item Item, ItemType Type, int Slot) {
    public override readonly string ToString() => $"{Name} ({Level}) x{Quantity}";

    public readonly string Name => Item.Name;
    public readonly int Level => (int)Item.Level;
    public readonly int Quantity => (int) Item.Quantity;
    public readonly bool HasSpecialType => Item.SpecialType != null;
}

public readonly record struct CachedEquipment(PlayerEquipment Equipment) {
    public readonly string Ring1 => Equipment.Ring1?.Name ?? string.Empty;
    public readonly string Ring2 => Equipment.Ring2?.Name ?? string.Empty;
    public readonly string Earring1 => Equipment.Earring1?.Name ?? string.Empty;
    public readonly string Earring2 => Equipment.Earring2?.Name ?? string.Empty;
    public readonly string Belt => Equipment.Belt?.Name ?? string.Empty;
    public readonly string MainHand => Equipment.MainHand?.Name ?? string.Empty;
    public readonly string OffHand => Equipment.OffHand?.Name ?? string.Empty;
    public readonly string Helmet => Equipment.Helmet?.Name ?? string.Empty;
    public readonly string Chest => Equipment.Chest?.Name ?? string.Empty;
    public readonly string Pants => Equipment.Pants?.Name ?? string.Empty;
    public readonly string Shoes => Equipment.Shoes?.Name ?? string.Empty;
    public readonly string Gloves => Equipment.Gloves?.Name ?? string.Empty;
    public readonly string Amulet => Equipment.Amulet?.Name ?? string.Empty;
    public readonly string Orb => Equipment.Orb?.Name ?? string.Empty;
    public readonly string Cape => Equipment.Cape?.Name ?? string.Empty;
}

public readonly record struct CachedEventMonster(
    Inbound.ServerInfo_EventMonsterType MonsterType,
    Inbound.ServerInfo_EventMonster Data,
    MapLocation Location)
{
    public bool EventIsLive => Data.IsLive;
    public bool MonsterIsAlive => Data.Health > 0;
    public string Type => MonsterType switch {
        Inbound.ServerInfo_EventMonsterType.BigAssCrab => GameConstants.BigAssCrabMobName,
        Inbound.ServerInfo_EventMonsterType.Dragold => GameConstants.DragoldMobName,
        Inbound.ServerInfo_EventMonsterType.Franky => GameConstants.FrankyMobName,
        Inbound.ServerInfo_EventMonsterType.Grinch => GameConstants.GrinchMobName,
        Inbound.ServerInfo_EventMonsterType.IceGolem => GameConstants.IceGolemMobName,
        Inbound.ServerInfo_EventMonsterType.MrGreen => GameConstants.MrGreenMobName,
        Inbound.ServerInfo_EventMonsterType.MrPumpkin => GameConstants.MrPumpkinMobName,
        Inbound.ServerInfo_EventMonsterType.PinkGoo => GameConstants.PinkGooMobName,
        Inbound.ServerInfo_EventMonsterType.Snowman => GameConstants.SnowmanMobName,
        Inbound.ServerInfo_EventMonsterType.Tiger => GameConstants.TigerMobName,
        Inbound.ServerInfo_EventMonsterType.Wabbit => GameConstants.WabbitMobName,
        _ => throw new()
    };
}
