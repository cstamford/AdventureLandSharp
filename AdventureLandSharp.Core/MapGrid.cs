using Faster.Map.QuadMap;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace AdventureLandSharp.Core;

public readonly record struct MapGridLineOfSight(
    MapGridCell Start,
    MapGridCell End,
    List<MapGridCell> Occluded
);

public readonly record struct MapGridPath(float Cost, List<MapGridCell> Points);

public readonly record struct MapGridPathSettings(MapGridHeuristic Heuristic,int? MaxSteps, float? MaxCost) {
    public MapGridPathSettings() : this(MapGridHeuristic.Euclidean, null, null) { }
}

public class MapGrid(GameDataMap map, GameLevelGeometry geo, GameDataSmap? smap) {
    public MapGridTerrain Terrain => _terrain;

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public MapGridPath IntraMap_AStar(MapGridCell start, MapGridCell goal, MapGridPathSettings settings) {
        Debug.Assert(_terrain[start].IsWalkable && _terrain[goal].IsWalkable, "IntraMap_AStar requires start and goal to be walkable.");
        Debug.Assert(start != goal, "IntraMap_AStar requires start and goal to be different.");

        QuadMap<MapGridCell, (float RunningCost, MapGridCell Cell)> dict = _dictPool.Value!;
        FastPriorityQueue<MapGridCell> Q = _queuePool.Value!;

        dict.Clear();
        dict.Emplace(start, (0, start));

        Q.Clear();
        Q.Enqueue(start, 0);

        for (int steps = 0; Q.TryDequeue(out MapGridCell pos, out float _) && pos != goal; ++steps) {
            float runningCost = dict[pos].RunningCost;

            if (settings.MaxSteps.HasValue && steps > settings.MaxSteps) {
                break;
            }

            if (settings.MaxCost.HasValue && runningCost > settings.MaxCost) {
                break;
            }

            foreach (MapGridCell offset in _neighbourOffsets) {
                MapGridCell neighbour = new(pos.X + offset.X, pos.Y + offset.Y);

                if (!_terrain[neighbour].IsWalkable) {
                    continue;
                }

                float costToNeighbour = pos.HeuristicCost(neighbour, settings.Heuristic) * _terrain[pos].Cost;
                float neighbourRunningCost = runningCost + costToNeighbour;
                bool exists = dict.Get(neighbour, out (float RunningCost, MapGridCell Cell) cur);

                if (!exists) {
                    dict.Emplace(neighbour, (neighbourRunningCost, pos));
                } else if (neighbourRunningCost < cur.RunningCost) {
                    dict.Update(neighbour, (neighbourRunningCost, pos));
                } else {
                    continue;
                }

                float neighbourTotalCost = neighbourRunningCost + neighbour.HeuristicCost(goal, settings.Heuristic);
                Q.Enqueue(neighbour, neighbourTotalCost);
            }
        }

        if (dict.Get(goal, out _)) {
            List<MapGridCell> path = [goal];

            MapGridCell current = goal;
            while (dict.Get(current, out (float _, MapGridCell Cell) cur) && cur.Cell != current) {
                path.Add(cur.Cell);
                current = cur.Cell;
            }

            path.Reverse();
            return new(dict[goal].RunningCost, path);
        }

        return new(float.MaxValue, []);
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public MapGridCell? FindNearestWalkable(MapGridCell start, MapGridPathSettings settings) {
        if (_terrain[start].IsWalkable) {
            return start;
        }

        QuadMap<MapGridCell, (float, MapGridCell)> dict = _dictPool.Value!;
        FastPriorityQueue<MapGridCell> Q = _queuePool.Value!;

        dict.Clear();
        dict.Emplace(start, default);

        Q.Clear();
        Q.Enqueue(start, 0);

        for (int steps = 0; Q.TryDequeue(out MapGridCell pos, out float cost); ++steps) {
            if (_terrain[pos].IsWalkable) {
                return pos;
            }

            if (steps > settings.MaxSteps) {
                break;
            }

            if (cost > settings.MaxCost) {
                break;
            }

            foreach (MapGridCell offset in _neighbourOffsets) {
                MapGridCell neighbour = new(pos.X + offset.X, pos.Y + offset.Y);
                if (!dict.Contains(neighbour)) {
                    Q.Enqueue(neighbour, neighbour.HeuristicCost(start, settings.Heuristic));
                    dict.Emplace(neighbour, default);
                }
            }
        }

        return null;
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public static MapGridLineOfSight LineOfSight(MapGridCell start, MapGridCell end, Func<MapGridCell, bool> getIsOccluded, int? maxResults = 1) {
        int x0 = start.X;
        int y0 = start.Y;
        int x1 = end.X;
        int y1 = end.Y;

        int dx = Math.Abs(x1 - x0);
        int dy = Math.Abs(y1 - y0);
        int sx = x0 < x1 ? 1 : -1;
        int sy = y0 < y1 ? 1 : -1;
        int err = dx - dy;

        List<MapGridCell> occluded = [];

        while (true) {
            MapGridCell cur = new(x0, y0);

            if (getIsOccluded(cur)) {
                occluded.Add(cur);
            }

            if (x0 == x1 && y0 == y1) {
                break;
            }

            if (occluded.Count >= maxResults) {
                break;
            }

            int e2 = 2 * err;

            if (e2 > -dy) {
                err -= dy;
                x0 += sx;
            }

            if (e2 < dx) {
                err += dx;
                y0 += sy;
            }
        }

        return new(start, end, occluded);
    }

    private readonly MapGridTerrain _terrain = new(map, geo, smap);

    private static readonly MapGridCell[] _neighbourOffsets = [ 
        new(-1,  0), new(1, 0), new(0, -1), new(0,  1),
        new(-1, -1), new(1, 1), new(-1, 1), new(1, -1)
    ];

    private static readonly ThreadLocal<QuadMap<MapGridCell, (float RunningCost, MapGridCell Cell)>> _dictPool = new(() => new());
    private static readonly ThreadLocal<FastPriorityQueue<MapGridCell>> _queuePool = new(() => new());
}
