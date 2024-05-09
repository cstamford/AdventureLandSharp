using Faster.Map.QuadMap;
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

public readonly record struct MapGridCell(ushort X, ushort Y) {
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public MapGridCell(int x, int y) : this((ushort)x, (ushort)y) 
    { }

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
    public override int GetHashCode() => X | (Y << 16);
}

public readonly record struct MapGridCellData(float Cost, float CornerScore) {
    public static MapGridCellData operator +(MapGridCellData lhs, float rhs) => lhs with { Cost = lhs.Cost + rhs };
    public static MapGridCellData operator +(MapGridCellData lhs, double rhs) => lhs with { Cost = lhs.Cost + (float)rhs };
    public static MapGridCellData Unwalkable => Default with { Cost = 0 };
    public static MapGridCellData Walkable => Default with { Cost = 1 };
    public static MapGridCellData Default => default;

    public readonly bool IsWalkable => Cost >= 1;
}

public readonly record struct MapGridPath(float Cost, List<MapGridCell> Points);

public readonly record struct MapGridPathSettings(MapGridHeuristic Heuristic,int? MaxSteps, float? MaxCost) {
    public MapGridPathSettings() : this(MapGridHeuristic.Euclidean, null, null) { }
}

public class MapGrid {
    public const int CellSize = 5;
    public static readonly float CellWorldEpsilon = MathF.Sqrt(CellSize*CellSize + CellSize*CellSize);
    public const int CellWallUnwalkable = CellSize/2;
    public const int CellWallAvoidance = CellWallUnwalkable + GameConstants.PlayerWidth/2;
    public const int CellWallSpatialQuery = CellWallAvoidance + 16;

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
    public bool IsWalkable(MapGridCell pos) => IsWithinBounds(pos) && _terrain[pos.X, pos.Y].IsWalkable;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsWalkable(Vector2 pos) => IsWalkable(WorldToGrid(pos));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float Cost(MapGridCell pos) => IsWithinBounds(pos) ? _terrain[pos.X, pos.Y].Cost : float.MaxValue;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float Cost(Vector2 pos) => Cost(WorldToGrid(pos));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float CornerScore(MapGridCell pos) => IsWithinBounds(pos) ? _terrain[pos.X, pos.Y].CornerScore : 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float CornerScore(Vector2 pos) => CornerScore(WorldToGrid(pos));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public MapGridCellData Data(MapGridCell pos) => IsWithinBounds(pos) ? _terrain[pos.X, pos.Y] : MapGridCellData.Unwalkable;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public MapGridCellData Data(Vector2 pos) => Data(WorldToGrid(pos));

    public MapGridPath IntraMap_AStar(Vector2 start, Vector2 goal, MapGridPathSettings settings) =>
        IntraMap_AStar(WorldToGrid(start), WorldToGrid(goal), settings);

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public MapGridPath IntraMap_AStar(MapGridCell start, MapGridCell goal, MapGridPathSettings settings) {
        Debug.Assert(IsWalkable(start) && IsWalkable(goal), "IntraMap_AStar requires start and goal to be walkable.");
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

                if (!IsWalkable(neighbour)) {
                    continue;
                }

                float costToNeighbour = pos.Cost(neighbour, settings.Heuristic) * Cost(neighbour);
                float neighbourRunningCost = runningCost + costToNeighbour;
                bool exists = dict.Get(neighbour, out (float RunningCost, MapGridCell Cell) cur);

                if (!exists) {
                    dict.Emplace(neighbour, (neighbourRunningCost, pos));
                } else if (neighbourRunningCost < cur.RunningCost) {
                    dict.Update(neighbour, (neighbourRunningCost, pos));
                } else {
                    continue;
                }

                float neighbourTotalCost = neighbourRunningCost + neighbour.Cost(goal, settings.Heuristic);
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
        if (IsWalkable(start)) {
            return start;
        }

        QuadMap<MapGridCell, (float, MapGridCell)> dict = _dictPool.Value!;
        FastPriorityQueue<MapGridCell> Q = _queuePool.Value!;

        dict.Clear();
        dict.Emplace(start, default);

        Q.Clear();
        Q.Enqueue(start, 0);

        for (int steps = 0; Q.TryDequeue(out MapGridCell pos, out float cost); ++steps) {
            if (IsWalkable(pos)) {
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
                    Q.Enqueue(neighbour, neighbour.Cost(start, settings.Heuristic));
                    dict.Emplace(neighbour, default);
                }
            }
        }

        return null;
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

    private static readonly ThreadLocal<QuadMap<MapGridCell, (float RunningCost, MapGridCell Cell)>> _dictPool = new(() => new());
    private static readonly ThreadLocal<FastPriorityQueue<MapGridCell>> _queuePool = new(() => new());

    [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
    private static MapGridCell WorldToGrid(GameLevelGeometry geo, double x, double y) 
        => WorldToGrid(geo, new((float)x, (float)y));

    [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
    private static MapGridCell WorldToGrid(GameLevelGeometry geo, Vector2 pos) => new((int)((pos.X - geo.MinX) / CellSize), (int)((pos.Y - geo.MinY) / CellSize));

    [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
    private static Vector2 GridToWorld(GameLevelGeometry geo, MapGridCell pos) => new(geo.MinX + pos.X * CellSize, geo.MinY + pos.Y * CellSize);

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

                // Create a spatial query covering the total area we care about overlaps in.
                IList<LineString> broadQuery = spatial.Query(new Envelope(
                    worldX - CellWallSpatialQuery,
                    worldX + CellSize + CellWallSpatialQuery,
                    worldY - CellWallSpatialQuery,
                    worldY + CellSize + CellWallSpatialQuery
                ));

                if (broadQuery.Count == 0) { // There's no overlap at all. We can just move on.
                    grid[x, y] = MapGridCellData.Walkable;
                    continue;
                }

                // Create geometry for the cell itself - we check this against anything overlapping the spatial query.
                // - Anything too close to the wall is marked as unwalkable.
                // - Anything within the wall avoidance distance is marked as walkable, but with a scaling penalty.
                Envelope cellEnvelope = new(worldX, worldX + CellSize, worldY, worldY + CellSize);
                Geometry cellGeometry = fac.ToGeometry(cellEnvelope);
                double dist = broadQuery.Min(l => l.Distance(cellGeometry));

                MapGridCellData cell = dist > CellWallUnwalkable ? 
                    MapGridCellData.Walkable + ((dist > CellWallAvoidance) ? 0 : (CellWallAvoidance - Math.Min(CellWallUnwalkable + dist, CellWallAvoidance)) / CellWallAvoidance) :
                    MapGridCellData.Unwalkable;

                // Calculate a score which represents "how much of a corner is this?".

                Coordinate center = new(worldX + CellSize/2, worldY + CellSize/2);
                LineString lineNW = fac.CreateLineString([center, new(worldX - CellWallSpatialQuery, worldY - CellWallSpatialQuery)]);
                LineString lineN = fac.CreateLineString([center, new(worldX, worldY - CellWallSpatialQuery)]); 
                LineString lineNE = fac.CreateLineString([center, new(worldX + CellWallSpatialQuery, worldY - CellWallSpatialQuery)]);
                LineString lineE = fac.CreateLineString([center, new(worldX + CellWallSpatialQuery, worldY)]);
                LineString lineSE = fac.CreateLineString([center, new(worldX + CellWallSpatialQuery, worldY + CellWallSpatialQuery)]);
                LineString lineS = fac.CreateLineString([center, new(worldX, worldY + CellWallSpatialQuery)]);
                LineString lineSW = fac.CreateLineString([center, new(worldX - CellWallSpatialQuery, worldY + CellWallSpatialQuery)]);
                LineString lineW = fac.CreateLineString([center, new(worldX - CellWallSpatialQuery, worldY)]);

                double NW = broadQuery.Min(l => l.Distance(lineNW));
                double N = broadQuery.Min(l => l.Distance(lineN));
                double NE = broadQuery.Min(l => l.Distance(lineNE));
                double E = broadQuery.Min(l => l.Distance(lineE));
                double SE = broadQuery.Min(l => l.Distance(lineSE));
                double S = broadQuery.Min(l => l.Distance(lineS));
                double SW = broadQuery.Min(l => l.Distance(lineSW));
                double W = broadQuery.Min(l => l.Distance(lineW));

                double maxDist = CellWallSpatialQuery*2 + CellSize/2;
        
                grid[x, y] = cell with { 
                    CornerScore = (float)((
                        (1 - NW / maxDist) + 
                        (1 - N / maxDist) + 
                        (1 - NE / maxDist) + 
                        (1 - E / maxDist) + 
                        (1 - SE / maxDist) + 
                        (1 - S / maxDist) +
                        (1 - SW / maxDist) +
                        (1 - W / maxDist)
                    ) / 8)
                };
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
                    grid[x, y] = MapGridCellData.Unwalkable;
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

            if (!grid[pos.X, pos.Y].IsWalkable || !reachableCells.Add(pos)) {
                continue;
            }

            queue.Enqueue(new(pos.X + 1, pos.Y));
            queue.Enqueue(new(pos.X - 1, pos.Y));
            queue.Enqueue(new(pos.X, pos.Y + 1));
            queue.Enqueue(new(pos.X, pos.Y - 1));
        }
    }
}

