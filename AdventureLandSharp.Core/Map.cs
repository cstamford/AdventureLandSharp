namespace AdventureLandSharp.Core;

public readonly record struct MapLocation(Map Map, Vector2 Location) : IComparable<MapLocation> {
    public readonly int CompareTo(MapLocation other) {
        int mapComparison = Map.Name.CompareTo(other.Map.Name);
        if (mapComparison != 0) return mapComparison;

        int xComparison = Location.X.CompareTo(other.Location.X);
        return xComparison != 0 ? xComparison : Location.Y.CompareTo(other.Location.Y);
    }

    public override readonly int GetHashCode() => HashCode.Combine(Map.Name, Location.X, Location.Y);

    public override readonly string ToString() => $"{Map.Name} {Location}";
}

public class Map {
    public Map(
        string mapName,
        ref readonly GameData gameData,
        ref readonly GameDataMap mapData,
        ref readonly GameLevelGeometry mapGeometry) {
        Name = mapName;
        _gameData = gameData;
        _mapData = mapData;
        _mapGeometry = mapGeometry;

        _connections = new(mapName, in gameData, in mapData);
        Grid = new(in mapData, in mapGeometry);
    }

    public string Name { get; }

    public MapGrid Grid { get; }

    public IReadOnlyList<MapConnection> Connections => _connections.Connections;
    public ref readonly GameDataMap Data => ref _mapData;
    public ref readonly GameLevelGeometry Geometry => ref _mapGeometry;

    public MapLocation DefaultSpawn => new(this,
        new((float)_mapData.SpawnPositions[0][0], (float)_mapData.SpawnPositions[0][1]));

    public float DefaultSpawnScatter =>
        _mapData.SpawnPositions[0].Length >= 3 ? (float)_mapData.SpawnPositions[0][3] : 0;

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public IMapGraphEdge[] FindPath(Vector2 start, Vector2 goal, MapGridHeuristic? heuristic = null) {
        Vector2 startWalkable = FindNearestWalkable(start);
        Vector2 goalWalkable = FindNearestWalkable(goal);

        (Vector2, Vector2) pathCacheKey = (startWalkable, goalWalkable);

        if (_pathCache.TryGetValue(pathCacheKey, out IEnumerable<IMapGraphEdge>? cachedPath)) // TODO: Dynamic costs...
            return cachedPath.Select(e => CopyEdgeWithRamp(e, start, goal)).ToArray();

        MapLocation startMapLoc = new(this, start);
        MapLocation goalMapLoc = new(this, goal);

        MapGridPathSettings settings = heuristic != null ? new(heuristic.Value) : new MapGridPathSettings();
        Task<MapGridPath> pathTask = Task.Run(() => Grid.IntraMap_AStar(startWalkable, goalWalkable, settings));
        Task<MapGridPath> teleportPathTask = Task.Run(() =>
            Grid.IntraMap_AStar(FindNearestWalkable(DefaultSpawn.Location), goalWalkable, settings));

        MapGridPath path = pathTask.Result;
        MapGridPath teleportPath = teleportPathTask.Result;

        if (path.Points.Count == 0 && teleportPath.Points.Count == 0) return [];

        float pathCost = pathTask.Result.Cost;
        float teleportCost = teleportPathTask.Result.Cost + 25;

        bool useRegularPath = path.Points.Count > 0 && (teleportPath.Points.Count == 0 || pathCost < teleportCost);

        MapGraphEdgeIntraMap pathEdge = useRegularPath
            ? new(startMapLoc, goalMapLoc, [..path.Points.Select(Grid.GridToWorld)], pathCost)
            : new MapGraphEdgeIntraMap(DefaultSpawn, goalMapLoc, [..teleportPath.Points.Select(Grid.GridToWorld)],
                teleportCost);

        IMapGraphEdge[] edges =
            useRegularPath ? [pathEdge] : [new MapGraphEdgeTeleport(startMapLoc, DefaultSpawn), pathEdge];

        _pathCache.TryAdd(pathCacheKey, edges);

        return edges.Select(e => CopyEdgeWithRamp(e, start, goal)).ToArray();
    }

    public Vector2 FindNearestWalkable(Vector2 world) => Grid.GridToWorld(Grid.FindNearestWalkable(Grid.WorldToGrid(world)));

    public IMapGraphEdge CopyEdgeWithRamp(IMapGraphEdge edge, Vector2 start, Vector2 goal) {
        switch (edge) {
            case MapGraphEdgeIntraMap intraEdge:
            {
                MapGraphEdgeIntraMap copy = intraEdge with {
                    Source = intraEdge.Source with { Location = start },
                    Dest = intraEdge.Dest with { Location = goal },
                    Path = [..intraEdge.Path]
                };

                if (intraEdge.Path[0] != start && Grid.IsWalkable(start)) copy.Path[0] = start;

                if (intraEdge.Path[^1] != goal && Grid.IsWalkable(goal)) copy.Path[^1] = goal;

                return copy;
            }
            case MapGraphEdgeTeleport teleportEdge:
                return teleportEdge with { Source = teleportEdge.Source with { Location = start } };
            default:
                throw new ArgumentException($"Unrecognised edge type: {edge.GetType()}");
        }
    }
    private readonly MapConnections _connections;

    private readonly GameData _gameData;
    private readonly GameDataMap _mapData;
    private readonly GameLevelGeometry _mapGeometry;

    private readonly ConcurrentDictionary<(Vector2, Vector2), IEnumerable<IMapGraphEdge>> _pathCache = [];
}
