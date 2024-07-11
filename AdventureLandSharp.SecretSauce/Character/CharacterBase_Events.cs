using System.Collections.Concurrent;
using AdventureLandSharp.Core;
using AdventureLandSharp.Core.SocketApi;
using AdventureLandSharp.Core.Util;

namespace AdventureLandSharp.SecretSauce.Character;

public abstract partial class CharacterBase {
    public SessionEventBusHandle EventBusHandle { 
        get => _eventBusHandle!;
        set => OnEventBusHandleInternal(value);
    }

    public IReadOnlyList<MapLocation> EventMapLocations => _eventMapLocations;
    public PriorityMobSpottedEvent? PriorityMobHuntEvent => _priorityMobHuntEvents.FirstOrNull();

    protected IReadOnlyDictionary<string, CharacterStatusEvent> CharacterStatuses => _characterStatuses;

    protected virtual void EventsUpdate() {
        DateTimeOffset now = DateTimeOffset.UtcNow;

        while (_incomingEvents.TryDequeue(out ISessionEvent? evt)) {
            if (_incomingEventsHandlers.TryGetValue(evt.GetType(), out Delegate? handler)) {
                handler.DynamicInvoke(evt);
            }
        }

        if (PriorityMob.HasValue) {
            AnnouncePriorityMob();
        }

        if (_magiportSentEvent.HasValue) {
            TimeSpan timeSinceSent = now.Subtract(_magiportSentTime);
            if (timeSinceSent >= TimeSpan.FromSeconds(1)) {
                Log.Warn("Magiport timed out!");
                _magiportSentEvent = null;
            } else if (timeSinceSent >= TimeSpan.FromSeconds(0.5) && Enemies.Any(x => x.Id == _magiportSentEvent.Value.MobId)) {
                Log.Info("Magiport arrived!");
                _magiportSentEvent = null;
            }
        }

        _priorityMobHuntEvents.RemoveAll(x => MyLoc.Equivalent(x.MobLocation) ||  EnemiesInRange.Any(y => y.Id == x.MobId));

        if (now.Subtract(_statusEventTime) >= TimeSpan.FromSeconds(5)) {
            EventBusHandle.Emit<CharacterStatusEvent>(new(this));
            _statusEventTime = now;
        }

        List<string> toRemove = [];

        foreach (CharacterStatusEvent evt in _characterStatuses.Values) {
            CachedPlayer? cached = Players.FirstOrNull(x => x.Id == evt.CharacterId);
            if (cached.HasValue) {
                _characterStatuses[evt.CharacterId] = new(
                    cached.Value.Id,
                    MyLoc with { Position = cached.Value.Position},
                    cached.Value.Player.StatusEffects
                );
            } else if (MyLoc.Equivalent(evt.Location, 128)) {
                toRemove.Add(evt.CharacterId);
            }
        }

        foreach (string id in toRemove) {
            _characterStatuses.Remove(id);
        }
    }

    protected virtual void OnEventBusHandle() {
        if (Cfg.ShouldAcceptMagiport) {
            RegisterEvent<MagiportOfferedEvent>(OnMagiportOffered);
            RegisterEvent<MagiportSentEvent>(OnMagiportSent);
        }

        if (Cfg.ShouldHuntPriorityMobs) {
            RegisterEvent<PriorityMobSpottedEvent>(OnPriorityMobSpottedEvent);
        }

        RegisterEvent<CharacterStatusEvent>(OnCharacterStatusUpdate);
    }

    private void OnEventBusHandleInternal(SessionEventBusHandle handle) {
        _eventBusHandle = handle;
        OnEventBusHandle();
        OnSocket();
        OnTactics();
        OnStrategy();
    }

    protected virtual void RegisterEvent<T>(Action<T> handler) where T : struct, ISessionEvent {
        _incomingEventsHandlers[typeof(T)] = handler;
        EventBusHandle.Register<T>(evt => _incomingEvents.Enqueue(evt));
    }

    private void OnMagiportOffered(MagiportOfferedEvent evt) {
        bool stateIsOk = !Withdrawing && !_magiportSentEvent.HasValue;
        bool alreadyFighting = evt.Mobs.Any(x => AttackTarget?.Id == x.MobId);
        bool anyHigherPriority = Enemies.Count == 0 || evt.Mobs.Any(x => Cfg.GetTargetPriority(x.MobType) > Enemies[0].Priority);
        bool isOkPriority = evt.Mobs.Any(x => Cfg.GetTargetPriorityType(x.MobType) >= TargetPriorityType.Normal);

        if (stateIsOk && !alreadyFighting && anyHigherPriority && isOkPriority) {
            if (IsTeleporting) {
                Socket.Emit<Outbound.Stop>(new("town"));
            }

            EventBusHandle.Emit<MagiportAcceptedEvent>(new(evt.Mobs.MaxBy(x => Cfg.GetTargetPriority(x.MobType)).MobId, Me.Name));
        }
    }

    private void OnMagiportSent(MagiportSentEvent evt) {
        if (evt.CharacterName == Me.Name) {
            _magiportSentTime = DateTimeOffset.UtcNow;
            _magiportSentEvent = evt;
            ResetMovement();
        }
    }

    private void OnPriorityMobSpottedEvent(PriorityMobSpottedEvent evt) {
        if (Cfg.GetTargetPriorityType(evt.MobType) == TargetPriorityType.Priority) {
            _priorityMobHuntEvents.Add(evt);
        }
    }

    private void OnCharacterStatusUpdate(CharacterStatusEvent evt) {
        _characterStatuses[evt.CharacterId] = evt;
    }

    private ConcurrentQueue<ISessionEvent> _incomingEvents = [];
    private Dictionary<Type, Delegate> _incomingEventsHandlers = [];

    private MagiportSentEvent? _magiportSentEvent;
    private DateTimeOffset _magiportSentTime;
    private SessionEventBusHandle? _eventBusHandle;

    private List<PriorityMobSpottedEvent> _priorityMobHuntEvents = [];
    private readonly Dictionary<string, DateTimeOffset> _priorityMobsAnnounced = [];
    private readonly List<(DateTimeOffset When, PriorityMobSpottedEvent Event)> _priorityMobSpottedEvents = [];

    private Dictionary<string, CharacterStatusEvent> _characterStatuses = [];
    private DateTimeOffset _statusEventTime = DateTimeOffset.UtcNow;
    private CachedMonster? PriorityMob => Enemies.FirstOrNull(x => 
        x.PriorityType == TargetPriorityType.Priority && (
            !_priorityMobsAnnounced.TryGetValue(x.Id, out DateTimeOffset t) || 
            DateTimeOffset.UtcNow.Subtract(t) >= TimeSpan.FromMinutes(1)
        ));

    private void AnnouncePriorityMob() {
        Monster mob = PriorityMob!.Value.Monster;
        MapLocation mobMapLocation = MyLoc with { Position = mob.Position };
        PriorityMobSpottedEvent evt = new(mob.Id, mob.Type, mobMapLocation);
        DateTimeOffset now = DateTimeOffset.UtcNow;
        _priorityMobsAnnounced[evt.MobId] = now;
        _priorityMobSpottedEvents.Add((now.AddSeconds(1), evt));
        EventBusHandle.Emit(evt);
    }
}

public record struct PriorityMobSpottedEvent(string MobId, string MobType, MapLocation MobLocation) : ISessionEvent;

public record struct MagiportOfferedEvent(List<(string MobId, string MobType)> Mobs) : ISessionEvent;
public record struct MagiportAcceptedEvent(string MobId, string CharacterName) : ISessionEvent;
public record struct MagiportSentEvent(string MobId, string CharacterName) : ISessionEvent;

public record struct CharacterStatusEvent(
    string CharacterId,
    MapLocation Location,
    StatusEffects StatusEffects) : ISessionEvent {

    public CharacterStatusEvent(CharacterBase character) : this(
        CharacterId: character.Entity.Id,
        Location: character.EntityLocation,
        StatusEffects: character.Entity.StatusEffects)
    { }
}
