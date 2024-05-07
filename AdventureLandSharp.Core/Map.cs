using System.Collections.Concurrent;
using System.Numerics;
using System.Runtime.CompilerServices;
using AdventureLandSharp.Core.Util;

namespace AdventureLandSharp.Core;

public readonly record struct MapLocation(Map Map, Vector2 Position) : IComparable<MapLocation> {
    public readonly override string ToString() => $"{Map.Name} {Position}";

    public readonly int CompareTo(MapLocation other) {
        int mapComparison = Map.Name.CompareTo(other.Map.Name);
        if (mapComparison != 0) return mapComparison;

        int xComparison = Position.X.CompareTo(other.Position.X);
        if (xComparison != 0) return xComparison;

        return Position.Y.CompareTo(other.Position.Y);
    }

    public readonly override int GetHashCode() => HashCode.Combine(Map.Name, Position.X, Position.Y);

    public readonly bool Equivalent(MapLocation other) => Map == other.Map && Position.Equivalent(other.Position);
    public readonly bool Equivalent(MapLocation other, float epsilon) => Map == other.Map && Position.Equivalent(other.Position, epsilon);
    public MapLocation AlignToGrid() => this with { Position = Position.AlignToGrid(Map) };
}

public class Map(string mapName, GameData gameData, GameDataMap mapData, GameLevelGeometry mapGeometry) {
    public string Name => mapName;
    public MapGrid Grid => _grid;
    public IReadOnlyList<MapConnection> Connections => _connections.Connections;
    public GameDataMap Data => mapData;
    public GameLevelGeometry Geometry => mapGeometry;
    public MapLocation DefaultSpawn => new(this, new((float)mapData.SpawnPositions[0][0], (float)mapData.SpawnPositions[0][1]));
    public MapLocation? FishingSpot => mapName == "main" ? new(this, new(-1368, -257)) : null;
    public float DefaultSpawnScatter => mapData.SpawnPositions[0].Length >= 4 ? (float)mapData.SpawnPositions[0][3] : 0;

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public MapGraphEdgeIntraMap? FindPath(Vector2 start, Vector2 goal, MapGridPathSettings? settings = null) {
        settings ??= new();

        MapGridPathSettings nearestWalkableSettings = settings.Value with { MaxSteps = 2048 };
        MapGridCell? startWalkableCell = Cache_GetNearestWalkable(start, nearestWalkableSettings);
        MapGridCell? goalWalkableCell = Cache_GetNearestWalkable(goal, nearestWalkableSettings);

        if (!startWalkableCell.HasValue || !goalWalkableCell.HasValue) {
            return null;
        }

        Vector2 startWalkable = startWalkableCell.Value.World(this);
        Vector2 goalWalkable = goalWalkableCell.Value.World(this);

        MapLocation startMapLoc = new(this, start);
        MapLocation goalMapLoc = new(this, goal);

        if (startWalkable == goalWalkable) {
            return start != goal ? new MapGraphEdgeIntraMap(startMapLoc, goalMapLoc, [start, goal], 0) : null;
        }

        if (_pathCache.TryGetValue((startWalkable, goalWalkable), out MapGraphEdgeIntraMap cachedEdge)) {
            return CopyEdgeWithRamp(cachedEdge, start, goal);
        }

        MapGridPath path = Grid.IntraMap_AStar(startWalkable, goalWalkable, settings ?? new());
        if (path.Points.Count == 0) {
            return null;
        }

        List<Vector2> merged = MergedGridPathToWorld(path.Points);
        List<Vector2> smoothed = SmoothedWorldPath(merged, divisions: 2);
        List<Vector2> smoothedReverse = [..smoothed];
        smoothedReverse.Reverse();

        MapGraphEdgeIntraMap edgeAB = new(startMapLoc, goalMapLoc, smoothed, path.Cost);
        MapGraphEdgeIntraMap edgeBA = new(goalMapLoc, startMapLoc, smoothedReverse, path.Cost);

        _pathCache.TryAdd((startWalkable, goalWalkable), edgeAB);
        _pathCache.TryAdd((goalWalkable, startWalkable), edgeBA);

        return CopyEdgeWithRamp(edgeAB, start, goal);
    }

    private readonly MapGrid _grid = new(mapData, mapGeometry);
    private readonly MapConnections _connections = new(mapName, gameData, mapData);
    private readonly ConcurrentDictionary<(Vector2, Vector2), MapGraphEdgeIntraMap> _pathCache = [];
    private readonly ConcurrentDictionary<MapGridCell, MapGridCell?> _walkableCache = [];

    private MapGridCell? Cache_GetNearestWalkable(Vector2 world, MapGridPathSettings settings) {
        MapGridCell cell = world.Grid(this);

        if (cell.IsWalkable(Grid)) {
            return cell;
        }

        if (_walkableCache.TryGetValue(cell, out MapGridCell? walkable)) {
            return walkable;
        }

        MapGridCell? nearest = Grid.FindNearestWalkable(cell, settings);
        _walkableCache.TryAdd(cell, nearest);
        return nearest;
    }

    private MapGraphEdgeIntraMap CopyEdgeWithRamp(MapGraphEdgeIntraMap edge, Vector2 start, Vector2 goal) {
        MapGraphEdgeIntraMap copy = edge with { 
            Source = new(edge.Source.Map, start),
            Dest = new(edge.Dest.Map, goal),
            Path = new(edge.Path)
        };

        if (edge.Path[0] != start && start.IsWalkable(Grid)) {
            copy.Path[0] = start;
        }

        if (edge.Path[^1] != goal && goal.IsWalkable(Grid)) {
            copy.Path[^1] = goal;
        }

        return copy;
    }

    private List<Vector2> MergedGridPathToWorld(List<MapGridCell> fullPath) {
        if (fullPath.Count <= 1) {
            return [..fullPath.Select(Grid.GridToWorld)];
        }

        List<Vector2> simplifiedPath = [fullPath[0].World(this)];

        for (int i = 0; i < fullPath.Count - 1; i++) {
            MapGridCell cur = fullPath[i];
            MapGridCell next = fullPath[i + 1];

            if (i < fullPath.Count - 2) {
                MapGridCell afterNext = fullPath[i + 2];
                if (Math.Abs(cur.X - afterNext.X) == 1 && Math.Abs(cur.Y - afterNext.Y) == 1) {
                    simplifiedPath.Add(afterNext.World(this));
                    i++; // Skip the next since it's folded into the diagonal
                    continue;
                }
            }

            simplifiedPath.Add(next.World(this));
        }

        return simplifiedPath;
    }

    private static List<Vector2> SmoothedWorldPath(IReadOnlyList<Vector2> path, int divisions) {
        if (path.Count < 3) {
            return [.. path];
        }

        List<Vector2> smoothedPath = [
            path[0],
            ..Enumerable.Repeat(Vector2.Zero, path.Count * divisions)];

        Parallel.For(0, path.Count, i => {
            Vector2 p0 = i == 0 ? path[i] : path[i - 1];
            Vector2 p1 = path[i];
            Vector2 p2 = i >= path.Count - 1 ? p1 : path[i + 1];
            Vector2 p3 = i >= path.Count - 2 ? p2 : path[i + 2];

            for (int division = 0; division < divisions; ++division) {
                float t = 1.0f / (divisions - 1) * (divisions - 1);
                int idx = divisions * i + division;
                smoothedPath[1 + idx] = GetBSplinePoint(p0, p1, p2, p3, t);
            }
        });

        return smoothedPath;
    }

    public static Vector2 GetCatmullRomPoint(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float t) {
        float t2 = t * t;
        float t3 = t2 * t;
        return 0.5f * ((2.0f * p1) + (-p0 + p2) * t + (2.0f * p0 - 5.0f * p1 + 4f * p2 - p3) * t2 + (-p0 + 3.0f * p1 - 3.0f * p2 + p3) * t3);
    }

    public static Vector2 GetBSplinePoint(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float t) {
        float it = 1.0f - t;
        float b0 = it * it * it / 6.0f;
        float b1 = (3 * t * t * t - 6 * t * t + 4) / 6.0f;
        float b2 = (-3 * t * t * t + 3 * t * t + 3 * t + 1) / 6.0f;
        float b3 = t * t * t / 6.0f;
        return b0 * p0 + b1 * p1 + b2 * p2 + b3 * p3;
    }
}

public static class Vector2Extensions {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Equivalent(this Vector2 a, Vector2 b) => a.Equivalent(b, MapGrid.CellWorldEpsilon);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Equivalent(this Vector2 a, Vector2 b, float epsilon) => a.SimpleDist(b) <= epsilon;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2 AlignToGrid(this Vector2 world, Map map) => world.Grid(map).World(map);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2 AlignToGrid(this Vector2 world, MapGrid grid) => world.Grid(grid).World(grid);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static MapGridCell Grid(this Vector2 world, Map map) => world.Grid(map.Grid);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static MapGridCell Grid(this Vector2 world, MapGrid grid) => grid.WorldToGrid(world);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsWalkable(this Vector2 world, Map map) => world.IsWalkable(map.Grid);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsWalkable(this Vector2 world, MapGrid grid) => grid.IsWalkable(world);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Cost(this Vector2 world, Map map) => world.Cost(map.Grid);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Cost(this Vector2 world, MapGrid grid) => grid.Cost(world);
}

public static class MapGridCellExtensions {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2 World(this MapGridCell cell, Map map) => cell.World(map.Grid);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2 World(this MapGridCell cell, MapGrid grid) => grid.GridToWorld(cell);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsWalkable(this MapGridCell cell, Map map) => cell.IsWalkable(map.Grid);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsWalkable(this MapGridCell cell, MapGrid grid) => grid.IsWalkable(cell);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Cost(this MapGridCell cell, Map map) => cell.Cost(map.Grid);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Cost(this MapGridCell cell, MapGrid grid) => grid.Cost(cell);
}

public static class MapLocationExtensions {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static MapGridCell Grid(this MapLocation loc) => loc.Position.Grid(loc.Map);
}
