using System.Diagnostics;
using System.Numerics;
using System.Text.Json;
using AdventureLandSharp.SecretSauce.Character;
using AdventureLandSharp.SecretSauce.Strategy;
using AdventureLandSharp.SecretSauce.Tactics;
using AdventureLandSharp.Core;
using AdventureLandSharp.Core.SocketApi;
using AdventureLandSharp.Core.Util;
using AdventureLandSharp.Helpers;
using AdventureLandSharp.Interfaces;
using AdventureLandSharp.Utility;

namespace AdventureLandSharp.SecretSauce.Classes;

public class Mage(World world, Socket socket, CharacterConfig config) : CharacterBase(world, socket, config) {
    public override CharacterClass Class => CharacterClass.Mage;

    protected override INode ActionBuild() => new Selector(
        // Don't pull until somebody else does, or it's almost dead, or we're very healthy
        new If(() => ActiveTactics is HighValue_PhoenixTactics &&
            AttackTarget!.Value.Monster.Target == string.Empty &&
            AttackTarget.Value.Monster.HealthPercent >= 25 &&
            Me.HealthPercent <= 75,
            new Success()
        ),
        base.ActionBuild()
    );

    protected override INode ClassBuild() => new Selector(
        _magiportOfferCd.IfThenDo(() => ReadyToOfferMagiport, OfferMagiport),
        new If(() => ReadyToMagiport, Skill("magiport", Magiport)),
        new If(() => ReadyToEnergize, Skill("energize", Energize))
    );

    protected override INode MovementBuild() => new Selector(
        new If(() => ReadyToBlink, Skill("blink", Blink)),
        new If(() => Me.MovementPlan is BlinkMovementPlan, new Success()),

        base.MovementBuild()
    );

    protected override CharacterLoadout DesiredLoadout => 
        TimeSinceAttackTargetLost <= TimeSpan.FromSeconds(2) && AttackTarget?.PriorityType >= TargetPriorityType.Priority ? 
            default(CharacterLoadout) with { MainHand = "firestaff" } :
            default(CharacterLoadout) with { MainHand = "pinkie" };

    protected override void OnEventBusHandle() {
        base.OnEventBusHandle();
        RegisterEvent<MagiportAcceptedEvent>(_magiportTargets.Enqueue);
    }

    protected override void OnSocket() {
        base.OnSocket();
        Socket.OnDisappear += OnSocket_Disappear;
        Socket.OnGameResponse += OnSocket_GameResponse;
        Socket.OnSkillTimeout += OnSocket;
    }

    protected override void OnStrategy() {
        base.OnStrategy();
        AvailableStrategies.Add(new HighValue_PhoenixScoutStrategy(this));
        AvailableStrategies.Add(new FarmLocationStrategy(Utils.GetMapLocationForSpawn(World, "squig")));
    }

    protected override void MovementUpdate() {
        base.MovementUpdate();
        if (Me.Mana >= 1600 && (!InCombat || Me.ManaPercent >= 50) && Me.MovementPlan is ClickAheadMovementPlan ca && DateTimeOffset.UtcNow >= _nextBlinkPermitted) {
            HandleBlinkPathConversion(ca);
        }
    }

    // TODO: This is OK, but flawed, because the real heuristic for path length with blink is not distance,
    // but rather the number of edges in the path. A fully optimized solution would run the inter-map only, presuming
    // 0 cost for each edge within the same map. Mages should use this when travelling and fall back to the regular
    // pathfinding when in combat.
    protected override IEnumerable<IMapGraphEdge> GenerateRoute(MapLocation start, MapLocation end, bool enableTeleport) {
        // Generate a route per normal, then merge any adjacent intra-map edges via direct routes.
        // This improves blinking performance.

        enableTeleport &= Me.Mana <= 1600;

        List<IMapGraphEdge> edges = [..base.GenerateRoute(start, end, enableTeleport)];
        if (edges.Count == 0) {
            Log.Warn($"Warning, no route found between {start} and {end} with enableTeleport={enableTeleport}. Regenerating with teleport.");
            edges = [..base.GenerateRoute(start, end, true)];
        }

        List<IMapGraphEdge> result = [];

        for (int i = 0; i < edges.Count; ++i) {
            IMapGraphEdge edge = edges[i];

            if (edge is MapGraphEdgeIntraMap) {
                int compatibleStartIdx = i;
                int compatibleEndIdx = i + 1;

                while (compatibleEndIdx < edges.Count && 
                    edges[compatibleEndIdx] is MapGraphEdgeIntraMap &&
                    edges[compatibleEndIdx].Source.Map == edge.Source.Map)
                {
                    ++compatibleEndIdx;
                }

                IMapGraphEdge mergeStartEdge = edges[compatibleStartIdx];
                IMapGraphEdge mergeEndEdge = edges[compatibleEndIdx - 1];

                if (mergeStartEdge == mergeEndEdge) {
                    result.Add(mergeStartEdge);
                } else {
                    Vector2 mergeStart = mergeStartEdge.Source.Position;
                    Vector2 mergeEnd = mergeEndEdge.Dest.Position;
                    MapGraphEdgeIntraMap? merged = mergeStartEdge.Source.Map.FindPath(mergeStart, mergeEnd);
                    Debug.Assert(merged.HasValue);
                    result.Add(merged.Value);
                }
            } else {
                result.Add(edge);
            }
        }

        Debug.Assert(edges.Count != 0, $"No route found between {start} and {end} with enableTeleport={enableTeleport}");

        return edges;
    }

    private bool ReadyToOfferMagiport => _magiportTargets.Count == 0 && Me.Mana >= 900 && MagiportMobs.Any();
    private bool ReadyToMagiport => _magiportTargets.Count > 0;
    private bool ReadyToEnergize => EnergizeCandidate?.Distance <= 320; 
    private bool ReadyToBlink => Me.Mana >= 1600 && Me.MovementPlan is BlinkMovementPlan;

    private CachedPlayer? EnergizeCandidate => 
        PartyPlayers.OrderByDescending(x => x.Player.ManaMissing).FirstOrNull();

    private IEnumerable<(string MobId, string MobType)> MagiportMobs => Enemies
        .Where(x => 
            x.PriorityType > TargetPriorityType.Blacklist && 
            x.Monster.HealthPercent >= 25 &&
            x.Distance < Me.AttackRange * 2)
        .Select(x => (x.Id, x.Type));

    private readonly Queue<MagiportAcceptedEvent> _magiportTargets = [];
    private DateTimeOffset _nextBlinkPermitted = DateTimeOffset.UtcNow;
    private readonly Cooldown _magiportOfferCd = new(TimeSpan.FromSeconds(1));

    private void OfferMagiport() => EventBusHandle.Emit<MagiportOfferedEvent>(new([..MagiportMobs]));

    private void Magiport() {
        // Ditch any nearby players (they don't need a summon!)
        while (_magiportTargets.Count > 0 && Players.Any(x => x.Player.Name == _magiportTargets.Peek().CharacterName)) {
            _magiportTargets.Dequeue();
        }

        // Send magiports to all people who want.
        while (_magiportTargets.TryPeek(out MagiportAcceptedEvent target)) {
            EventBusHandle.Emit<MagiportSentEvent>(new(target.MobId, target.CharacterName));
            Socket.Emit<Outbound.UseSkillOnId>(new("magiport", target.CharacterName));
            _magiportTargets.Dequeue();
        }
    }

    private void Energize() => Socket.Emit<Outbound.UseSkillEnergize>(new(
        EnergizeCandidate!.Value.Player.Id, 
        (int)Math.Min(EnergizeCandidate.Value.Player.ManaMissing, Math.Max(50, Me.Mana / 5))
    ));

    private void Blink() {
        Debug.Assert(Me.MovementPlan is BlinkMovementPlan);
        BlinkMovementPlan blink = (BlinkMovementPlan)Me.MovementPlan;
        Socket.Emit<Outbound.UseSkillOnPosition>(new("blink", blink.BlinkingTarget.X, blink.BlinkingTarget.Y));
    }

    private void OnSocket(Inbound.SkillTimeoutData evt) {
        if (evt.SkillName == "blink") {
            _nextBlinkPermitted = DateTimeOffset.UtcNow.AddMilliseconds(Math.Max(1000, evt.MillisecondsUntilReady));
        }
    }

    private void OnSocket_Disappear(JsonElement evt) {
        if (evt.GetString("id") != Me.Id) {
            return;
        }

        bool isBlink = evt.TryGetProperty("effect", out JsonElement eff) && 
            eff.ValueKind == JsonValueKind.String && 
            eff.GetString() == "blink";

        bool hasDest = evt.TryGetProperty("s", out JsonElement dest) && 
            dest.ValueKind == JsonValueKind.Array;

        if (isBlink && hasDest) {
            Vector2 position = new(dest[0].GetSingle(), dest[1].GetSingle());

            if (Me.MovementPlan is BlinkMovementPlan blink) {
                IEnumerable<Vector2> path = [blink.BlinkingTarget, blink.ActualTarget];
                Me.MovementPlan = new ClickAheadMovementPlan(position, new(path), MyLoc.Map);
            } else {
                ResetMovement();
            }
        };
    }

    private void OnSocket_GameResponse(JsonElement evt) {
        if (evt.GetString("response") == "blink_failed") {
            ResetMovement();
            _nextBlinkPermitted = DateTimeOffset.UtcNow.AddSeconds(2);
        }
    }

    private void HandleBlinkPathConversion(ClickAheadMovementPlan ca) {
        float distance = ca.Path.Zip(ca.Path.Prepend(MyLoc.Position), (a, b) => a.SimpleDist(b)).Sum();
        float distanceCutoff = Me.Speed*6;

        if (distance >= distanceCutoff) {
            Vector2 actualTarget = ca.Path.Last();

            MapGridCell? blinkingTarget = MapGrid.FindNearest(actualTarget.Grid(MyLoc.Map), pos => {
                MapGridCellData data = pos.Data(MyLoc.Map);
                return data.IsWalkable && data.RpHashScore == 0 && data.PHashScore == 0;
            }, new());

            if (blinkingTarget.HasValue) {
                Me.MovementPlan = new BlinkMovementPlan(ca.Goal, actualTarget, blinkingTarget.Value.World(MyLoc.Map));
            }
        }
    }
}

public class BlinkMovementPlan(Vector2 movingTo, Vector2 actualTarget, Vector2 blinkingTarget) : ISocketEntityMovementPlan {
    public override string ToString() => $"Blinking from {movingTo} to {blinkingTarget}";

    public bool Finished => false;
    public Vector2 Position => movingTo;
    public Vector2 Goal => movingTo;
    public Vector2 ActualTarget => actualTarget;
    public Vector2 BlinkingTarget => blinkingTarget;
    public void Update(double dt, double speed) { }
}
