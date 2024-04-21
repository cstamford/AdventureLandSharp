using NetTopologySuite;
using NetTopologySuite.Geometries;
using NetTopologySuite.Index.Strtree;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace AdventureLandSharp.Core;

public enum MapGridHeuristic {
    Manhattan,
    Euclidean,
    Diagonal
}

public readonly record struct MapGridLineOfSight(
    MapGridCell Start,
    MapGridCell End,
    MapGridCell? OccludedAt
);

public readonly record struct MapGridCell(int X, int Y) : IComparable<MapGridCell> {
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public MapGridCell(Vector2 grid)
        : this((int)(grid.X + 0.5f), (int)(grid.Y + 0.5f)) { }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public float Cost(MapGridCell other, MapGridHeuristic heuristic) => heuristic switch {
        MapGridHeuristic.Manhattan => ManhattanDistance(this, other),
        MapGridHeuristic.Euclidean => EuclideanDistance(this, other),
        MapGridHeuristic.Diagonal => DiagonalDistance(this, other),
        _ => throw new ArgumentOutOfRangeException(nameof(heuristic))
    };

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static float ManhattanDistance(MapGridCell lhs, MapGridCell rhs) {
        return MathF.Abs(lhs.X - rhs.X) + MathF.Abs(lhs.Y - rhs.Y);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static float EuclideanDistance(MapGridCell lhs, MapGridCell rhs) {
        float dx = lhs.X - rhs.X;
        float dy = lhs.Y - rhs.Y;
        return MathF.Sqrt(dx * dx + dy * dy);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static float DiagonalDistance(MapGridCell lhs, MapGridCell rhs) {
        float dmax = MathF.Max(MathF.Abs(lhs.X - rhs.X), MathF.Abs(lhs.Y - rhs.Y));
        float dmin = MathF.Min(MathF.Abs(lhs.X - rhs.X), MathF.Abs(lhs.Y - rhs.Y));
        return 1.4142136f * dmin + (dmax - dmin);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public int CompareTo(MapGridCell other) {
        int xComparison = X.CompareTo(other.X);
        return xComparison == 0 ? Y.CompareTo(other.Y) : xComparison;
    }
}

public readonly record struct MapGridCellData(bool Walkable, float Cost);

public readonly record struct MapGridPath(float Cost, List<MapGridCell> Points);

public readonly record struct MapGridPathSettings(MapGridHeuristic Heuristic, IReadOnlyDictionary<MapGridCell, float>? DynamicCosts) {
    public MapGridPathSettings() : this(MapGridHeuristic.Diagonal, null) { }
    public MapGridPathSettings(MapGridHeuristic heuristic) : this(heuristic, null) { }
    public MapGridPathSettings(IReadOnlyDictionary<MapGridCell, float> dynamicCosts) : this(MapGridHeuristic.Diagonal, dynamicCosts) { }
}

public class MapGrid {
    public const int CellSize = 8;
    public const int CellWallUnwalkable = 6;
    public const int CellWallAvoidance = CellSize*2;
    public static readonly float CellWorldEpsilon = MathF.Sqrt(CellSize*CellSize + CellSize*CellSize);

    public MapGrid(GameDataMap mapData, GameLevelGeometry mapGeometry) {
        _terrain = CreateTerrain(mapData, mapGeometry);
        _width = _terrain.GetLength(0);
        _height = _terrain.GetLength(1);
        _mapGeometry = mapGeometry;
    }

    public MapGridCellData[,] Terrain => _terrain;
    public int Width => _width;
    public int Height => _height;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public MapGridCell WorldToGrid(double x, double y) => WorldToGrid(_mapGeometry, x, y);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public MapGridCell WorldToGrid(Vector2 pos) => WorldToGrid(_mapGeometry, pos);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector2 GridToWorld(MapGridCell pos) => GridToWorld(_mapGeometry, pos);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsWithinBounds(MapGridCell pos) => pos.X >= 0 && pos.X < _width && pos.Y >= 0 && pos.Y < _height;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsWithinBounds(Vector2 pos) => IsWithinBounds(WorldToGrid(pos));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsWalkable(MapGridCell pos) => IsWithinBounds(pos) && _terrain[pos.X, pos.Y].Walkable;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsWalkable(Vector2 pos) => IsWalkable(WorldToGrid(pos));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float Cost(MapGridCell pos) => IsWithinBounds(pos) ? _terrain[pos.X, pos.Y].Cost : float.MaxValue;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float Cost(Vector2 pos) => Cost(WorldToGrid(pos));

    public MapGridPath IntraMap_AStar(Vector2 start, Vector2 goal, MapGridPathSettings? settings = null) =>
        IntraMap_AStar(WorldToGrid(start), WorldToGrid(goal), settings);

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public MapGridPath IntraMap_AStar(MapGridCell start, MapGridCell goal, MapGridPathSettings? settings = null) {
        Debug.Assert(IsWalkable(start) && IsWalkable(goal), "IntraMap_AStar requires start and goal to be walkable.");

        if (start == goal) {
            return new(0, []);
        }

        MapGridHeuristic heuristic = (settings ?? new()).Heuristic;
        IReadOnlyDictionary<MapGridCell, float> dynamicCosts = settings?.DynamicCosts ?? new Dictionary<MapGridCell, float>();

        PriorityQueue<MapGridCell, float> queue = new();
        HashSet<MapGridCell> closed = [];

        Dictionary<MapGridCell, MapGridCell> backtrack = [];
        Dictionary<MapGridCell, float> runningCosts = new() { [start] = 0 };

        queue.Enqueue(start, 0);

        while (queue.TryDequeue(out MapGridCell pos, out float _) && pos != goal) {
            float runningCost = runningCosts[pos];
            closed.Add(pos);

            foreach (MapGridCell offset in _neighbourOffsets) {
                MapGridCell neighbour = new(pos.X + offset.X, pos.Y + offset.Y);

                if (!IsWalkable(neighbour) || closed.Contains(neighbour)) {
                    continue;
                }

                float costToNeighbour = pos.Cost(neighbour, heuristic) * Cost(neighbour);

                if (dynamicCosts.TryGetValue(neighbour, out float dynamicCost)) {
                    costToNeighbour *= dynamicCost;
                }

                float neighbourRunningCost = runningCost + costToNeighbour;
                float neighbourTotalCost = neighbourRunningCost + neighbour.Cost(goal, heuristic);

                if (!runningCosts.TryGetValue(neighbour, out float currentRunningCost) || neighbourRunningCost < currentRunningCost) {
                    runningCosts[neighbour] = neighbourRunningCost;
                    backtrack[neighbour] = pos;
                    queue.Enqueue(neighbour, (float)neighbourTotalCost);
                }
            }
        }

        List<MapGridCell> backtrackPath = [];
        MapGridCell backtrackPos = goal;

        while (backtrack.TryGetValue(backtrackPos, out MapGridCell backtrackPrevPos)) {
            backtrackPath.Add(backtrackPos);
            backtrackPos = backtrackPrevPos;
        }

        if (backtrackPos != start) {
            return new(float.MaxValue, []);
        }

        backtrackPath.Add(start);
        backtrackPath.Reverse();

        return new(runningCosts[goal], backtrackPath);
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public MapGridCell FindNearestWalkable(MapGridCell start, MapGridPathSettings? settings = null) {
        if (IsWalkable(start)) {
            return start;
        }

        MapGridHeuristic heuristic = (settings ?? new()).Heuristic;
        PriorityQueue<MapGridCell, float> queue = new();
        HashSet<MapGridCell> closed = [];

        queue.Enqueue(start, 0);
        closed.Add(start);

        while (queue.TryDequeue(out MapGridCell pos, out _)) {
            if (IsWalkable(pos)) {
                return pos;
            }

            foreach (MapGridCell offset in _neighbourOffsets) {
                MapGridCell neighbour = new(pos.X + offset.X, pos.Y + offset.Y);
                if (!closed.Contains(neighbour)) {
                    queue.Enqueue(neighbour, neighbour.Cost(start, heuristic));
                    closed.Add(neighbour);
                }
            }
        }

        throw new Exception("No walkable cell found.");
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public MapGridLineOfSight LineOfSight(MapGridCell start, MapGridCell end, bool costChangeIsOccluder = false) {
        int x0 = start.X;
        int y0 = start.Y;
        int x1 = end.X;
        int y1 = end.Y;

        int dx = Math.Abs(x1 - x0);
        int dy = Math.Abs(y1 - y0);
        int sx = x0 < x1 ? 1 : -1;
        int sy = y0 < y1 ? 1 : -1;
        int err = dx - dy;

        MapGridCell last = new(x0, y0);
        float cost = Cost(last);

        while (true) {
            MapGridCell cur = new(x0, y0);

            if (!IsWalkable(cur) || (costChangeIsOccluder && cost != Cost(cur))) {
                return new(start, end, last);
            }

            if (x0 == x1 && y0 == y1) {
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

            last = cur;
        }

        return new(start, end, null);
    }


    private readonly MapGridCellData[,] _terrain;
    private readonly int _width;
    private readonly int _height;
    private readonly GameLevelGeometry _mapGeometry;
    private static readonly MapGridCell[] _neighbourOffsets = [ 
        new(-1,  0), new(1, 0), new(0, -1), new(0,  1),
        new(-1, -1), new(1, 1), new(-1, 1), new(1, -1)
    ];

    [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
    private static MapGridCell WorldToGrid(GameLevelGeometry geo, double x, double y) 
        => WorldToGrid(geo, new((float)x, (float)y));

    [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
    private static MapGridCell WorldToGrid(GameLevelGeometry geo, Vector2 pos) => new(
        (int)MathF.Round((pos.X - geo.MinX) / CellSize), 
        (int)MathF.Round((pos.Y - geo.MinY) / CellSize));

    [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
    private static Vector2 GridToWorld(GameLevelGeometry geo, MapGridCell pos) => new(
        geo.MinX + pos.X * CellSize,
        geo.MinY + pos.Y * CellSize);

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private static MapGridCellData[,] CreateTerrain(GameDataMap map, GameLevelGeometry geo) {
        STRtree<LineString> spatial = new();

        foreach (int[] line in geo.XLines ?? []) {
            LineString lineString = new([new(line[0], line[1]), new(line[0], line[2])]);
            spatial.Insert(lineString.EnvelopeInternal, lineString);
        }

        foreach (int[] line in geo.YLines ?? []) {
            LineString lineString = new([new(line[1], line[0]), new(line[2], line[0])]);
            spatial.Insert(lineString.EnvelopeInternal, lineString);
        }

        spatial.Build();

        int width = (geo.MaxX - geo.MinX) / CellSize;
        int height = (geo.MaxY - geo.MinY) / CellSize;
        MapGridCellData[,] grid = new MapGridCellData[width, height];

        GeometryFactory fac = NtsGeometryServices.Instance.CreateGeometryFactory();

        Parallel.For(0, width, x => {
            for (int y = 0; y < height; ++y) {
                int worldX = geo.MinX + x * CellSize;
                int worldY = geo.MinY + y * CellSize;

                Envelope cellEnvelope = new(worldX, worldX + CellSize, worldY, worldY + CellSize);
                Geometry cellGeometry = fac.ToGeometry(cellEnvelope);

                IList<LineString> query = spatial.Query(new Envelope(
                    worldX - CellSize - CellWallAvoidance - CellWallUnwalkable,
                    worldX + CellSize + CellSize + CellWallAvoidance + CellWallUnwalkable,
                    worldY - CellSize - CellWallAvoidance - CellWallUnwalkable,
                    worldY + CellSize + CellSize + CellWallAvoidance + CellWallUnwalkable
                ));

                bool walkable = true;
                float cost = 1.0f;

                if (query.Count > 0) {
                    walkable = !query.Any(l => l.Intersects(cellGeometry));
                    double dist = query.Min(l => l.Distance(new Point(worldX + CellSize / 2, worldY + CellSize / 2)));
                    const float avoidance = CellWallAvoidance * 2;
                    cost += (float)((avoidance - Math.Min(dist, avoidance)) / avoidance);
                }

                grid[x, y] = new MapGridCellData(walkable, cost);
            }
        });

        HashSet<MapGridCell> reachableCells = [];

        foreach (double[] spawn in map.SpawnPositions) {
            Vector2 spawnPoint = new((float)spawn[0], (float)spawn[1]);
            MapGridCell GridLocation = WorldToGrid(geo, spawnPoint);
            IterativeFloodFill(GridLocation, grid, reachableCells);
        }

        for (int x = 0; x < width; ++x) {
            for (int y = 0; y < height; ++y) {
                if (!reachableCells.Contains(new MapGridCell(x, y))) {
                    grid[x, y] = grid[x, y] with { Walkable = false };
                }
            }
        }

        return grid;
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private static void IterativeFloodFill(MapGridCell startPos, MapGridCellData[,] grid, HashSet<MapGridCell> reachableCells) {
        Queue<MapGridCell> queue = [];
        queue.Enqueue(startPos);

        while (queue.Count > 0) {
            MapGridCell pos = queue.Dequeue();

            if (pos.X < 0 || pos.X >= grid.GetLength(0) || pos.Y < 0 || pos.Y >= grid.GetLength(1)) {
                continue;
            }

            if (!grid[pos.X, pos.Y].Walkable || !reachableCells.Add(pos)) {
                continue;
            }

            queue.Enqueue(new(pos.X + 1, pos.Y));
            queue.Enqueue(new(pos.X - 1, pos.Y));
            queue.Enqueue(new(pos.X, pos.Y + 1));
            queue.Enqueue(new(pos.X, pos.Y - 1));
        }
    }
}

