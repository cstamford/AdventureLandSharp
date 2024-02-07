using System.Numerics;

namespace AdventureLandSharp.Util;

public static class AStar {
    public record PathResult(List<GridPos> Path, int Steps, Dictionary<GridPos, int> DebugVisitedCount);

    public readonly record struct MapTerrainCell(
        bool Walkable,
        float Cost);

    public readonly record struct GridPos(int X, int Y) : IComparable<GridPos> {
        public GridPos(Vector2 grid)
            : this((int)(grid.X + 0.5f), (int)(grid.Y + 0.5f)) { }

        public Vector2 Vec2 => new(X, Y);

        public double Cost(GridPos other) => EuclideanDistance(this, other);

        public static double ManhattanDistance(GridPos lhs, GridPos rhs) {
            return Math.Abs(lhs.X - rhs.X) + Math.Abs(lhs.Y - rhs.Y);
        }

        public static double EuclideanDistance(GridPos lhs, GridPos rhs) {
            double dx = lhs.X - rhs.X;
            double dy = lhs.Y - rhs.Y;
            return Math.Sqrt(dx * dx + dy * dy);
        }

        public static double DiagonalDistance(GridPos lhs, GridPos rhs) {
            double dmax = Math.Max(Math.Abs(lhs.X - rhs.X), Math.Abs(lhs.Y - rhs.Y));
            double dmin = Math.Min(Math.Abs(lhs.X - rhs.X), Math.Abs(lhs.Y - rhs.Y));
            return 1.4142136f * dmin + (dmax - dmin);
        }

        public int CompareTo(GridPos other) {
            int xComparison = X.CompareTo(other.X);
            return xComparison == 0 ? Y.CompareTo(other.Y) : xComparison;
        }
    }

    public static PathResult FindPath(MapTerrainCell[,] grid, GridPos start, GridPos goal, bool debug) {
        PriorityQueue<GridPos> queue = new();
        HashSet<GridPos> closed = [];

        // TODO: Not four dictionaries, holy hell.
        Dictionary<GridPos, GridPos> backtrack = new();
        Dictionary<GridPos, double> runningCosts = new();
        Dictionary<GridPos, GridPos> directions = new();
        Dictionary<GridPos, int> debugVisitedCount = new();

        runningCosts[start] = 0;
        queue.Enqueue(start, 0);

        for (int steps = 0; queue.TryDequeue(out GridPos pos, out double _); ++steps) {
            if (pos == goal) {
                List<GridPos> path = [goal];

                while (backtrack.TryGetValue(pos, out GridPos prev)) {
                    path.Add(prev);
                    pos = prev;
                }

                path.Reverse();
                return new(path, steps, debugVisitedCount);
            }

            if (debug) {
                debugVisitedCount.TryGetValue(pos, out int count);
                debugVisitedCount[pos] = count + 1;
            }

            double runningCost = runningCosts[pos];

            closed.Add(pos);

            VisitPoint(new(pos.X - 1, pos.Y), new(-1, 0));
            VisitPoint(new(pos.X + 1, pos.Y), new(1, 0));
            VisitPoint(new(pos.X, pos.Y - 1), new(0, -1));
            VisitPoint(new(pos.X, pos.Y + 1), new(0, 1));
            VisitPoint(new(pos.X - 1, pos.Y - 1), new(-1, -1));
            VisitPoint(new(pos.X + 1, pos.Y + 1), new(1, 1));
            VisitPoint(new(pos.X - 1, pos.Y + 1), new(-1, 1));
            VisitPoint(new(pos.X + 1, pos.Y - 1), new(1, -1));

            void VisitPoint(GridPos neighbour, GridPos direction) {
                bool bounds = neighbour.X >= 0 &&
                    neighbour.X < grid.GetLength(0) &&
                    neighbour.Y >= 0 &&
                    neighbour.Y < grid.GetLength(1);

                if (!bounds) {
                    return;
                }

                bool walkable = grid[neighbour.X, neighbour.Y].Walkable;

                if (!walkable) {
                    return;
                }

                if (Math.Abs(direction.X) == 1 && Math.Abs(direction.Y) == 1) {
                    bool diagonalBounds = pos.X + direction.X >= 0 &&
                        pos.X + direction.X < grid.GetLength(0) &&
                        pos.Y + direction.Y >= 0 &&
                        pos.Y + direction.Y < grid.GetLength(1);

                    if (!diagonalBounds) {
                        return;
                    }

                    bool diagonalWalkable = grid[pos.X + direction.X, pos.Y].Walkable &&
                        grid[pos.X, pos.Y + direction.Y].Walkable;

                    if (!diagonalWalkable) {
                        return;
                    }
                }

                if (closed.Contains(neighbour)) {
                    return;
                }

                // TODO: If we're diagonal, we should include some part of the fixed cost of the grids we might overlap.
                double costToNeighbour = pos.Cost(neighbour) * grid[neighbour.X, neighbour.Y].Cost;

                if (directions.TryGetValue(pos, out GridPos prevDirection) && prevDirection != direction) {
                    costToNeighbour *= 1.25;
                }

                double neighbourRunningCost = runningCost + costToNeighbour;
                double neighbourTotalCost = neighbourRunningCost + neighbour.Cost(goal);

                if (!runningCosts.TryGetValue(neighbour, out double currentRunningCost) || neighbourRunningCost < currentRunningCost) {
                    runningCosts[neighbour] = neighbourRunningCost;
                    backtrack[neighbour] = pos;
                    directions[neighbour] = direction;

                    if (!queue.TryUpdatePriority(neighbour, neighbourTotalCost)) {
                        queue.Enqueue(neighbour, neighbourTotalCost);
                    }
                }
            }
        }

        return new([], 0, []);
    }

    public static List<GridPos> GetLine(GridPos start, GridPos end) {
        List<GridPos> ret = [];

        int dx = Math.Abs(end.X - start.X), dy = Math.Abs(end.Y - start.Y);
        int sx = (start.X < end.X) ? 1 : -1, sy = (start.Y < end.Y) ? 1 : -1;
        int err = dx - dy;

        int x = start.X;
        int y = start.Y;

        while (true) {
            ret.Add(new GridPos(x, y));

            if (x == end.X && y == end.Y) {
                break;
            }

            int e2 = err * 2;

            if (e2 > -dy) {
                err -= dy;
                x += sx;
            }

            if (e2 < dx) {
                err += dx;
                y += sy;
            }
        }

        return ret;
    }
}
