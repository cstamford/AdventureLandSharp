using System.Numerics;
using AdventureLandSharp.Core.Util;

namespace AdventureLandSharp.Core.SocketApi;

public interface ISocketEntityMovementPlan {
    public bool Finished { get; }
    public Vector2 Position { get; }
    public Vector2 Goal { get; }
    public void Update(double dt, double speed);
}

public interface ISocketEntityMovementPlanModulator : ISocketEntityMovementPlan {
    public ISocketEntityMovementPlan Plan { get; }
}

public class DestinationMovementPlan(Vector2 start, Vector2 goal) : ISocketEntityMovementPlan {
    public override string ToString() => $"Position={Position}, Goal={Goal}, Finished={Finished}";

    public bool Finished => start == goal;
    public Vector2 Position => start;
    public Vector2 Goal => goal;

    public void Update(double dt, double speed) {
        Vector2 dir = Vector2.Normalize(goal - start);
        float distance = goal.SimpleDist(start);
        float step = (float)(speed * dt);
        start = step < distance ? start + dir * step : goal;
    }
}

public class PathMovementPlan(Vector2 start, Queue<Vector2> path) : ISocketEntityMovementPlan {
    public override string ToString() => $"Position={Position}, Goal={Goal}, Finished={Finished}, {Path.Count} steps left";

    public Queue<Vector2> Path => path;
    public bool Finished => path.Count == 0;
    public Vector2 Position => start;
    public Vector2 Goal => path.TryPeek(out Vector2 goal) ? goal : start;

    public void Update(double dt, double speed) {
        while (dt > 0 && path.TryPeek(out Vector2 subgoal)) {
            Vector2 dir = Vector2.Normalize(subgoal - start);
            float distance = subgoal.SimpleDist(start);
            float step = (float)(speed * dt);

            if (step < distance) {
                start += dir * step;
                break;
            }

            start = subgoal;
            dt -= distance / speed;
            path.Dequeue();
        }
    }
}

public class ClickAheadMovementPlan(Vector2 start, Queue<Vector2> path, Map map) : ISocketEntityMovementPlan {
    public override string ToString() => $"{_pathMovementPlan}, _clickAheadPoint={_clickAheadPoint}";

    public IReadOnlyCollection<Vector2> Path => _pathMovementPlan.Path;
    public bool Finished => _pathMovementPlan.Finished;
    public Vector2 Position  => _pathMovementPlan.Position;
    public Vector2 Goal => _clickAheadPoint;
    public Vector2 OriginalGoal => _pathMovementPlan.Goal;

    public void Update(double dt, double speed) { 
        _pathMovementPlan.Update(dt, speed);
        _clickAheadPoint = Path.Count > 0 ? CalculateClickAheadPoint(OriginalGoal, (float)speed) : OriginalGoal;
    }

    private readonly PathMovementPlan _pathMovementPlan = new(start, path);
    private Vector2 _clickAheadPoint = start;
    private static readonly TimeSpan _clickAheadLatency = TimeSpan.FromMilliseconds(200);

    private Vector2 CalculateClickAheadPoint(Vector2 target, float speed) {
        Vector2 direction = Vector2.Normalize(target - Position);
        Vector2 clickAheadTarget = target + direction * speed * (float)_clickAheadLatency.TotalSeconds;
        MapGridLineOfSight los = map.Grid.LineOfSight(Position.Grid(map), clickAheadTarget.Grid(map));
        return los.OccludedAt?.World(map) ?? clickAheadTarget;
    }
}
