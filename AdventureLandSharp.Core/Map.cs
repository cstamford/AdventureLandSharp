using System.Collections.Concurrent;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace AdventureLandSharp.Core;

public class Map(string mapName, GameData gameData, GameDataMap mapData, GameLevelGeometry mapGeometry, GameDataSmap? smapData) {
    public string Name => mapName;
    public MapGrid Grid => _grid;
    public IReadOnlyList<MapConnection> Connections => _connections.Connections;
    public GameDataMap Data => mapData;
    public GameDataSmap? Smap => smapData;
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

        if (startWalkable == goalWalkable) {
            return start != goal ? new MapGraphEdgeIntraMap(startWalkable, goalWalkable, this, [start, goal], 0) : null;
        }

        if (_pathCache.TryGetValue((startWalkable, goalWalkable), out MapGraphEdgeIntraMap cachedEdge)) {
            return CopyEdgeWithRamp(cachedEdge, start, goal);
        }

        MapGridPath path = Grid.IntraMap_AStar(startWalkableCell.Value, goalWalkableCell.Value, settings ?? new());
        Debug.Assert(path.Points.All(x => x.RpHash(this).IsValid));

        if (path.Points.Count == 0) {
            return null;
        }

        List<Vector2> pathWorld = [.. path.Points.Select(Grid.Terrain.World)];
        Debug.Assert(pathWorld.All(x => x.RpHash(this).IsValid));

        List<Vector2> pathWorldReversed = [.. pathWorld];
        pathWorldReversed.Reverse();

        MapGraphEdgeIntraMap edgeAB = new(startWalkable, goalWalkable, this, pathWorld, path.Cost);
        MapGraphEdgeIntraMap edgeBA = new(goalWalkable, startWalkable, this, pathWorldReversed, path.Cost);

        _pathCache.TryAdd((startWalkable, goalWalkable), edgeAB);
        _pathCache.TryAdd((goalWalkable, startWalkable), edgeBA);

        return CopyEdgeWithRamp(edgeAB, start, goal);
    }

    private readonly MapGrid _grid = new(mapData, mapGeometry, smapData);
    private readonly MapConnections _connections = new(mapName, gameData, mapData);
    private readonly ConcurrentDictionary<(Vector2, Vector2), MapGraphEdgeIntraMap> _pathCache = [];
    private readonly ConcurrentDictionary<MapGridCell, MapGridCell?> _walkableCache = [];

    private MapGridCell? Cache_GetNearestWalkable(Vector2 world, MapGridPathSettings settings) {
        MapGridCell cell = world.Grid(this);

        if (cell.Data(this).IsWalkable) {
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
        MapGraphEdgeIntraMap copy = edge with { SourcePos = start, DestPos = goal, Path = new(edge.Path) };

        if (edge.Path[0] != start && start.Data(this).IsWalkable) {
            copy.Path[0] = start;
        }

        if (edge.Path[^1] != goal && goal.Data(this).IsWalkable) {
            copy.Path[^1] = goal;
        }

        return copy;
    }
}
