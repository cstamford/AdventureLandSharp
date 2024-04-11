using System.Numerics;

namespace AdventureLandSharp.Core.SocketApi;

public interface ISocketEntityMovementPlan {
    public bool Finished { get; }
    public Vector2 Position { get; }
    public Vector2 Goal { get; }
    public bool Update(double dt, double speed);
}

public class DestinationMovementPlan(Vector2 start, Vector2 goal) : ISocketEntityMovementPlan {
    public bool Finished => start == goal;
    public Vector2 Position => start;
    public Vector2 Goal => goal;

    public bool Update(double dt, double speed) {
        Vector2 dir = Vector2.Normalize(goal - start);
        float distance = Vector2.Distance(goal, start);
        float step = (float)(speed * dt);
        start = step >= distance ? goal : dir * step;
        return Finished;
    }
}

public class PathMovementPlan(Vector2 start, Queue<Vector2> path) : ISocketEntityMovementPlan {
    public Queue<Vector2> Path => new(path);
    public bool Finished => path.Count == 0;
    public Vector2 Position => start;
    public Vector2 Goal => path.TryPeek(out Vector2 goal) ? goal : start;

    public bool Update(double dt, double speed) {
        while (dt > 0 && path.TryPeek(out Vector2 subgoal)) {
            Vector2 dir = Vector2.Normalize(subgoal - start);
            float distance = Vector2.Distance(subgoal, start);
            float step = (float)(speed * dt);

            if (step < distance) {
                start += dir * step;
                break;
            }

            start = subgoal;
            dt -= distance / speed;
            path.Dequeue();
        }

        return Finished;
    }
}