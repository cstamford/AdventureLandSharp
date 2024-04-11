
using System.Collections.Concurrent;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace AdventureLandSharp.Core;

public readonly record struct MapLocation(Map Map, Vector2 Location) : IComparable<MapLocation> {
    public readonly int CompareTo(MapLocation other) {
        int mapComparison = Map.Name.CompareTo(other.Map.Name);
        if (mapComparison != 0) return mapComparison;

        int xComparison = Location.X.CompareTo(other.Location.X);
        if (xComparison != 0) return xComparison;

        return Location.Y.CompareTo(other.Location.Y);
    }

    public readonly override int GetHashCode() => HashCode.Combine(Map.Name, Location.X, Location.Y);

    public readonly override string ToString() => $"{Map.Name} {Location}";
}

public class Map(string mapName, GameData gameData, GameDataMap mapData, GameLevelGeometry mapGeometry) {
    public string Name => mapName;
    public MapGrid Grid => _grid;
    public IReadOnlyList<MapConnection> Connections => _connections.Connections;
    public GameDataMap Data => mapData;
    public GameLevelGeometry Geometry => mapGeometry;
    public MapLocation DefaultSpawn => new(this, new((float)mapData.SpawnPositions[0][0], (float)mapData.SpawnPositions[0][1]));
    public float DefaultSpawnScatter => mapData.SpawnPositions[0].Length >= 3 ? (float)mapData.SpawnPositions[0][3] : 0;

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public IEnumerable<IMapGraphEdge> FindPath(Vector2 start, Vector2 goal, MapGridHeuristic? heuristic = null) {
        Vector2 startWalkable = FindNearestWalkable(start);
        Vector2 goalWalkable = FindNearestWalkable(goal);

        (Vector2, Vector2) pathCacheKey = (startWalkable, goalWalkable);

        if (_pathCache.TryGetValue(pathCacheKey, out IEnumerable<IMapGraphEdge>? cachedPath)) { // TODO: Dynamic costs...
            return cachedPath.Select(e => CopyEdgeWithRamp(e, start, goal));
        }

        MapLocation startMapLoc = new(this, start);
        MapLocation goalMapLoc = new(this, goal);

        MapGridPathSettings settings = heuristic != null ? new(heuristic.Value) : new();
        Task<MapGridPath> pathTask = Task.Run(() => Grid.IntraMap_AStar(startWalkable, goalWalkable, settings));
        Task<MapGridPath> teleportPathTask = Task.Run(() => Grid.IntraMap_AStar(FindNearestWalkable(DefaultSpawn.Location), goalWalkable, settings));

        MapGridPath path = pathTask.Result;
        MapGridPath teleportPath = teleportPathTask.Result;

        if (path.Points.Count == 0 && teleportPath.Points.Count == 0) {
            return [];
        }

        float pathCost = pathTask.Result.Cost;
        float teleportCost = teleportPathTask.Result.Cost + 25;

        bool useRegularPath = path.Points.Count > 0 && (teleportPath.Points.Count == 0 || pathCost < teleportCost);

        MapGraphEdgeIntraMap pathEdge = useRegularPath ? 
            new MapGraphEdgeIntraMap(startMapLoc, goalMapLoc, [..path.Points.Select(Grid.GridToWorld)], pathCost) :
            new MapGraphEdgeIntraMap(DefaultSpawn, goalMapLoc, [..teleportPath.Points.Select(Grid.GridToWorld)], teleportCost);

        IEnumerable<IMapGraphEdge> edges = useRegularPath ? 
            [pathEdge] :
            [new MapGraphEdgeTeleport(startMapLoc, DefaultSpawn), pathEdge];

        _pathCache.TryAdd(pathCacheKey, edges);

        return edges.Select(e => CopyEdgeWithRamp(e, start, goal));
    }

    public Vector2 FindNearestWalkable(Vector2 world) => _grid.GridToWorld(_grid.FindNearestWalkable(_grid.WorldToGrid(world)));

    private readonly MapGrid _grid = new(mapData, mapGeometry);
    private readonly MapConnections _connections = new(mapName, gameData, mapData);
    private readonly ConcurrentDictionary<(Vector2, Vector2), IEnumerable<IMapGraphEdge>> _pathCache = [];

    public IMapGraphEdge CopyEdgeWithRamp(IMapGraphEdge edge, Vector2 start, Vector2 goal) {
        if (edge is MapGraphEdgeIntraMap intraEdge) {
            MapGraphEdgeIntraMap copy = intraEdge with { 
                Source = new(intraEdge.Source.Map, start),
                Dest = new(intraEdge.Dest.Map, goal),
                Path = new(intraEdge.Path)
            };

            if (intraEdge.Path[0] != start && Grid.IsWalkable(start)) {
                copy.Path[0] = start;
            }

            if (intraEdge.Path[^1] != goal && Grid.IsWalkable(goal)) {
                copy.Path[^1] = goal;
            }

            return copy;
        }

        if (edge is MapGraphEdgeTeleport teleportEdge) {
            return teleportEdge with { Source = new(teleportEdge.Source.Map, start) };
        } 

        throw new ArgumentException($"Unrecognised edge type: {edge.GetType()}");
    }
}
