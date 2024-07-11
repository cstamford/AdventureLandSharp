using System.Diagnostics;
using AdventureLandSharp.Core;
using AdventureLandSharp.Core.Util;
using AdventureLandSharp.Helpers;
using AdventureLandSharp.Utility;

namespace AdventureLandSharp.SecretSauce.Character;

public abstract partial class CharacterBase {
    protected virtual INode MovementBuild() => new Selector(
        new If(() => _magiportSentEvent.HasValue, new Success()),

        new If(() => InCombat && PositioningPlan is not NullPositioningPlan, new Selector(
            _movementCd.ThenDo(UpdateAttackMovementTarget),
            new Success()
        )),

        new If(() => IsTeleporting, new Success()),
        _movementCd.ThenDo(UpdateMovementTarget)
    );

    protected virtual void MovementUpdate() {
        float extraTimeToAdd = MyLoc.Position.SimpleDist(Movement?.End.Position ?? MyLoc.Position);
        _movementCd.Duration = TimeSpan.FromMilliseconds(100 + Math.Min(extraTimeToAdd, 900));

    DateTimeOffset now = DateTimeOffset.UtcNow;
        bool shouldDebug = now >= _nextMovementDebugTime;

        if (shouldDebug && Log.LogLevelEnabled(LogLevel.Debug)) {
            Log.Debug(MovementStateDebugString);
            _nextMovementDebugTime = now.AddSeconds(1);
        }

        if (now.Subtract(_lastPositionChangeTime) >= TimeSpan.FromSeconds(15) && now >= _nextMovementResetTime) {
            ResetMovement();
            _nextMovementResetTime = now.AddSeconds(30);
        }

        _movementBt.Tick();
        Movement?.Update();

        if (shouldDebug && Log.LogLevelEnabled(LogLevel.DebugVerbose)) {
            Log.DebugVerbose($"(after update) {MovementStateDebugString}");
        }
    }

    protected MapGraphTraversal? Movement { get; set; }
    protected bool IsTeleporting => Movement?.Finished == false && Movement?.CurrentEdge is MapGraphEdgeTeleport;
    protected string MovementStateDebugString => 
        $"MyLoc: {MyLoc}, " + 
        $"MyLocLast: {MyLocLast}, " +
        $"TargetMapLocation: {TargetMapLocation}, " +
        $"MovementPlan: {Me.MovementPlan}, " +
        $"MapGraphTraversal: {Movement}, " +
        $"_lastPositionChangeTime: {_lastPositionChangeTime}";

    private DateTimeOffset _nextMovementDebugTime = DateTimeOffset.UtcNow;
    private DateTimeOffset _nextMovementResetTime = DateTimeOffset.UtcNow;

    protected bool UpdateMovement(MapLocation tarLoc) {
        bool regenerate = ShouldRegeneratePath(tarLoc);

        if (regenerate) {
            IEnumerable<IMapGraphEdge> route = GenerateRoute(MyLoc, tarLoc, !InCombat);
            MapGraphTraversal movement = new(Socket, route, tarLoc);
            ResetMovement(movement);
        }

        return regenerate;
    }

    protected bool ShouldRegeneratePath(MapLocation tarLoc) {
        bool alreadyThere = MyLoc.Equivalent(tarLoc);
        bool alreadyGeneratedThisEnd = Movement?.End.Equivalent(tarLoc) ?? false;
        bool alreadyFinished = Movement?.Finished ?? false;
        bool regenBecauseShouldBeFinished = !alreadyThere && alreadyFinished;
        bool regenBecauseDifferentEnd = !alreadyThere && !alreadyGeneratedThisEnd;
        return MyLoc.RpHash().IsValid && (regenBecauseShouldBeFinished || regenBecauseDifferentEnd);
    }

    protected void ResetMovement(MapGraphTraversal? movement = null) {
        Movement = movement;
        Me.MovementPlan = null;
    }

    protected virtual IEnumerable<IMapGraphEdge> GenerateRoute(MapLocation start, MapLocation end, bool enableTeleport) =>
        World.FindRoute(start, end, new MapGraphPathSettings() with {
            EnableTeleport = enableTeleport,
            EnableEvents = _eventJoins
        });

    private Status UpdateAttackMovementTarget() {
        IPositioningPlan plan = PositioningPlan;
        Debug.Assert(plan is not NullPositioningPlan);

        _positioningPlanWeights.Clear();
        plan.StoreWeights(_positioningPlanWeights);
        GridWeight weight = plan.GetPosition(_positioningPlanWeights);

        MapLocation goal = MyLoc with { Position = weight.Grid.World(MyLoc.Map) };
        UpdateMovement(goal);

        return Status.Success;
    }

    private Status UpdateMovementTarget() {
        MapLocation tarLoc = TargetMapLocation;
        if (ShouldRegeneratePath(tarLoc)) {
            Log.Info($"Going to movement target {tarLoc}");
        }

        UpdateMovement(tarLoc);
        return Status.Running;
    }

    private readonly INode _movementBt;
    private readonly Cooldown _movementCd = new(TimeSpan.Zero);
    private DateTimeOffset _lastPositionChangeTime;
}

