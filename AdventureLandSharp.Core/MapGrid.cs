namespace AdventureLandSharp.Core;

public enum MapGridHeuristic
{
    Manhattan,
    Euclidean,
    Diagonal
}

public readonly record struct MapGridCell(int X, int Y) : IComparable<MapGridCell>
{
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public MapGridCell(Vector2 grid)
        : this((int) (grid.X + 0.5f), (int) (grid.Y + 0.5f))
    {
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public int CompareTo(MapGridCell other)
    {
        var xComparison = X.CompareTo(other.X);
        return xComparison == 0 ? Y.CompareTo(other.Y) : xComparison;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public float Cost(MapGridCell other, MapGridHeuristic heuristic)
    {
        return heuristic switch
        {
            MapGridHeuristic.Manhattan => ManhattanDistance(this, other),
            MapGridHeuristic.Euclidean => EuclideanDistance(this, other),
            MapGridHeuristic.Diagonal => DiagonalDistance(this, other),
            _ => throw new ArgumentOutOfRangeException(nameof(heuristic))
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static float ManhattanDistance(MapGridCell lhs, MapGridCell rhs)
    {
        return MathF.Abs(lhs.X - rhs.X) + MathF.Abs(lhs.Y - rhs.Y);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static float EuclideanDistance(MapGridCell lhs, MapGridCell rhs)
    {
        float dx = lhs.X - rhs.X;
        float dy = lhs.Y - rhs.Y;
        return MathF.Sqrt(dx * dx + dy * dy);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static float DiagonalDistance(MapGridCell lhs, MapGridCell rhs)
    {
        var dmax = MathF.Max(MathF.Abs(lhs.X - rhs.X), MathF.Abs(lhs.Y - rhs.Y));
        var dmin = MathF.Min(MathF.Abs(lhs.X - rhs.X), MathF.Abs(lhs.Y - rhs.Y));
        return 1.4142136f * dmin + (dmax - dmin);
    }
}

public readonly record struct MapGridCellData(bool Walkable, float Cost);

public readonly record struct MapGridPath(float Cost, List<MapGridCell> Points);

public readonly record struct MapGridPathSettings(
    MapGridHeuristic Heuristic,
    IReadOnlyDictionary<MapGridCell, float>? DynamicCosts)
{
    public MapGridPathSettings() : this(MapGridHeuristic.Diagonal, null)
    {
    }

    public MapGridPathSettings(MapGridHeuristic heuristic) : this(heuristic, null)
    {
    }

    public MapGridPathSettings(IReadOnlyDictionary<MapGridCell, float> dynamicCosts) : this(MapGridHeuristic.Diagonal,
        dynamicCosts)
    {
    }
}

public class MapGrid
{
    public const int CellSize = 8;
    public const int CellWallUnwalkable = 6;
    public const int CellWallAvoidance = CellSize;

    private static readonly MapGridCell[] NeighbourOffsets =
    [
        new MapGridCell(-1, 0), new MapGridCell(1, 0), new MapGridCell(0, -1), new MapGridCell(0, 1),
        new MapGridCell(-1, -1), new MapGridCell(1, 1), new MapGridCell(-1, 1), new MapGridCell(1, -1)
    ];

    private readonly GameLevelGeometry _mapGeometry;


    public MapGrid(ref readonly GameDataMap mapData, ref readonly GameLevelGeometry mapGeometry)
    {
        Terrain = CreateTerrain(mapData, mapGeometry);
        Width = Terrain.GetLength(0);
        Height = Terrain.GetLength(1);
        _mapGeometry = mapGeometry;
    }

    public MapGridCellData[,] Terrain { get; }

    public int Width { get; }

    public int Height { get; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public MapGridCell WorldToGrid(double x, double y)
    {
        return WorldToGrid(_mapGeometry, x, y);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public MapGridCell WorldToGrid(Vector2 pos)
    {
        return WorldToGrid(_mapGeometry, pos);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector2 GridToWorld(MapGridCell pos)
    {
        return GridToWorld(_mapGeometry, pos);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsWithinBounds(MapGridCell pos)
    {
        return pos.X >= 0 && pos.X < Width && pos.Y >= 0 && pos.Y < Height;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsWithinBounds(Vector2 pos)
    {
        return IsWithinBounds(WorldToGrid(pos));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsWalkable(MapGridCell pos)
    {
        return IsWithinBounds(pos) && Terrain[pos.X, pos.Y].Walkable;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsWalkable(Vector2 pos)
    {
        return IsWalkable(WorldToGrid(pos));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float Cost(MapGridCell pos)
    {
        return IsWithinBounds(pos) ? Terrain[pos.X, pos.Y].Cost : float.MaxValue;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float Cost(Vector2 pos)
    {
        return Cost(WorldToGrid(pos));
    }

    public MapGridPath IntraMap_AStar(Vector2 start, Vector2 goal, MapGridPathSettings? settings = null)
    {
        return IntraMap_AStar(WorldToGrid(start), WorldToGrid(goal), settings);
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public MapGridPath IntraMap_AStar(MapGridCell start, MapGridCell goal, MapGridPathSettings? settings = null)
    {
        Debug.Assert(IsWalkable(start) && IsWalkable(goal), "IntraMap_AStar requires start and goal to be walkable.");

        if (start == goal) return new MapGridPath(0, []);

        var heuristic = (settings ?? new MapGridPathSettings()).Heuristic;
        var dynamicCosts = settings?.DynamicCosts ?? new Dictionary<MapGridCell, float>();

        PriorityQueue<MapGridCell, float> queue = new();
        HashSet<MapGridCell> closed = [];

        Dictionary<MapGridCell, MapGridCell> backtrack = [];
        Dictionary<MapGridCell, float> runningCosts = new() {[start] = 0};

        queue.Enqueue(start, 0);

        while (queue.TryDequeue(out var position, out var _) && position != goal)
        {
            var runningCost = runningCosts[position];
            closed.Add(position);

            foreach (var offset in NeighbourOffsets)
            {
                MapGridCell neighbour = new(position.X + offset.X, position.Y + offset.Y);

                if (!IsWalkable(neighbour) || closed.Contains(neighbour)) continue;

                var costToNeighbour = position.Cost(neighbour, heuristic) * Cost(neighbour);

                if (dynamicCosts.TryGetValue(neighbour, out var dynamicCost)) costToNeighbour *= dynamicCost;

                var neighbourRunningCost = runningCost + costToNeighbour;
                var neighbourTotalCost = neighbourRunningCost + neighbour.Cost(goal, heuristic);

                if (runningCosts.TryGetValue(neighbour, out var currentRunningCost) &&
                    !(neighbourRunningCost < currentRunningCost)) continue;

                runningCosts[neighbour] = neighbourRunningCost;
                backtrack[neighbour] = position;
                queue.Enqueue(neighbour, neighbourTotalCost);
            }
        }

        List<MapGridCell> backtrackPath = [];
        var backtrackPosition = goal;

        while (backtrack.TryGetValue(backtrackPosition, out var backtrackPrevPos))
        {
            backtrackPath.Add(backtrackPosition);
            backtrackPosition = backtrackPrevPos;
        }

        if (backtrackPosition != start) return new MapGridPath(float.MaxValue, []);

        backtrackPath.Add(start);
        backtrackPath.Reverse();

        return new MapGridPath(runningCosts[goal], backtrackPath);
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public MapGridCell FindNearestWalkable(MapGridCell start, MapGridPathSettings? settings = null)
    {
        if (IsWalkable(start)) return start;

        var heuristic = (settings ?? new MapGridPathSettings()).Heuristic;
        PriorityQueue<MapGridCell, float> queue = new();
        HashSet<MapGridCell> closed = [];

        queue.Enqueue(start, 0);
        closed.Add(start);

        while (queue.TryDequeue(out var position, out _))
        {
            if (IsWalkable(position)) return position;

            foreach (var offset in NeighbourOffsets)
            {
                MapGridCell neighbour = new(position.X + offset.X, position.Y + offset.Y);
                if (!closed.Contains(neighbour))
                {
                    queue.Enqueue(neighbour, neighbour.Cost(start, heuristic));
                    closed.Add(neighbour);
                }
            }
        }

        throw new Exception("No walkable cell found.");
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public bool CheckLineOfSight(MapGridCell start, MapGridCell end)
    {
        var x0 = start.X;
        var y0 = start.Y;
        var x1 = end.X;
        var y1 = end.Y;

        var dx = Math.Abs(x1 - x0);
        var dy = Math.Abs(y1 - y0);
        var sx = x0 < x1 ? 1 : -1;
        var sy = y0 < y1 ? 1 : -1;
        var err = dx - dy;

        while (true)
        {
            MapGridCell cur = new(x0, y0);

            if (!IsWalkable(cur)) return false;

            if (x0 == x1 && y0 == y1) break;

            var e2 = 2 * err;
            if (e2 > -dy)
            {
                err -= dy;
                x0 += sx;
            }

            if (e2 >= dx) continue;

            err += dx;
            y0 += sy;
        }

        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
    private static MapGridCell WorldToGrid(GameLevelGeometry geo, double x, double y)
    {
        return WorldToGrid(geo, new Vector2((float) x, (float) y));
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
    private static MapGridCell WorldToGrid(GameLevelGeometry geo, Vector2 pos)
    {
        return new MapGridCell(
            (int) MathF.Round((pos.X - geo.MinX) / CellSize),
            (int) MathF.Round((pos.Y - geo.MinY) / CellSize));
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
    private static Vector2 GridToWorld(GameLevelGeometry geo, MapGridCell pos)
    {
        return new Vector2(
            geo.MinX + pos.X * CellSize,
            geo.MinY + pos.Y * CellSize);
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private static MapGridCellData[,] CreateTerrain(GameDataMap map, GameLevelGeometry geo)
    {
        STRtree<LineString> spatial = new();

        foreach (var line in geo.XLines ?? [])
        {
            LineString lineString = new([new Coordinate(line[0], line[1]), new Coordinate(line[0], line[2])]);
            spatial.Insert(lineString.EnvelopeInternal, lineString);
        }

        foreach (var line in geo.YLines ?? [])
        {
            LineString lineString = new([new Coordinate(line[1], line[0]), new Coordinate(line[2], line[0])]);
            spatial.Insert(lineString.EnvelopeInternal, lineString);
        }

        spatial.Build();

        var width = (geo.MaxX - geo.MinX) / CellSize;
        var height = (geo.MaxY - geo.MinY) / CellSize;
        var grid = new MapGridCellData[width, height];

        var fac = NtsGeometryServices.Instance.CreateGeometryFactory();

        Parallel.For(0, width, x =>
        {
            for (var y = 0; y < height; ++y)
            {
                var worldX = geo.MinX + x * CellSize;
                var worldY = geo.MinY + y * CellSize;

                Envelope cellEnvelope = new(worldX, worldX + CellSize, worldY, worldY + CellSize);
                var cellGeometry = fac.ToGeometry(cellEnvelope);

                var query = spatial.Query(new Envelope(
                    worldX - CellSize - CellWallAvoidance - CellWallUnwalkable,
                    worldX + CellSize + CellSize + CellWallAvoidance + CellWallUnwalkable,
                    worldY - CellSize - CellWallAvoidance - CellWallUnwalkable,
                    worldY + CellSize + CellSize + CellWallAvoidance + CellWallUnwalkable
                ));

                var walkable = true;
                var cost = 1.0f;

                if (query.Count > 0)
                {
                    walkable = !query.Any(l => l.Intersects(cellGeometry));
                    var dist = query.Min(l => l.Distance(new Point(worldX + CellSize / 2, worldY + CellSize / 2)));
                    const float avoidance = CellWallAvoidance * 2;
                    cost += (float) ((avoidance - Math.Min(dist, avoidance)) / avoidance);
                }

                grid[x, y] = new MapGridCellData(walkable, cost);
            }
        });

        HashSet<MapGridCell> reachableCells = [];

        foreach (var spawn in map.SpawnPositions)
        {
            Vector2 spawnPoint = new((float) spawn[0], (float) spawn[1]);
            var gridLocation = WorldToGrid(geo, spawnPoint);
            IterativeFloodFill(gridLocation, grid, reachableCells);
        }

        for (var x = 0; x < width; ++x)
        for (var y = 0; y < height; ++y)
            if (!reachableCells.Contains(new MapGridCell(x, y)))
                grid[x, y] = grid[x, y] with {Walkable = false};

        return grid;
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private static void IterativeFloodFill(MapGridCell startPos, MapGridCellData[,] grid,
        HashSet<MapGridCell> reachableCells)
    {
        Queue<MapGridCell> queue = [];
        queue.Enqueue(startPos);

        while (queue.Count > 0)
        {
            var pos = queue.Dequeue();

            if (pos.X < 0 || pos.X >= grid.GetLength(0) || pos.Y < 0 || pos.Y >= grid.GetLength(1)) continue;

            if (!grid[pos.X, pos.Y].Walkable || !reachableCells.Add(pos)) continue;

            queue.Enqueue(new MapGridCell(pos.X + 1, pos.Y));
            queue.Enqueue(new MapGridCell(pos.X - 1, pos.Y));
            queue.Enqueue(new MapGridCell(pos.X, pos.Y + 1));
            queue.Enqueue(new MapGridCell(pos.X, pos.Y - 1));
        }
    }
}