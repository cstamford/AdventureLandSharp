using System.Runtime.CompilerServices;

namespace AdventureLandSharp.Core;

public class World {
    public World(GameData data) {
        _data = data;
        _maps = data.Maps
            .Select<KeyValuePair<string, GameDataMap>, (string, GameDataMap, GameLevelGeometry)?>(x => 
                data.Maps.TryGetValue(x.Key, out GameDataMap map) && 
                data.Geometry.TryGetValue(x.Key, out GameLevelGeometry geo) ? 
                (x.Key, map, geo) : null)
            .Where(x => x.HasValue)
            .Select(x => new Map(x!.Value.Item1, data, x.Value.Item2, x.Value.Item3))
            .ToDictionary(map => map.Name);

        _mapsGraph = new(_maps);
    }

    public GameData Data => _data;
    public IEnumerable<KeyValuePair<string, Map>> Maps => _maps;

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public IEnumerable<IMapGraphEdge> FindRoute(MapLocation start, MapLocation goal, MapGridHeuristic? heuristic = null) {
        start = new(start.Map, start.Map.FindNearestWalkable(start.Location));
        goal = new(goal.Map, goal.Map.FindNearestWalkable(goal.Location));
        return _mapsGraph.InterMap_Djikstra(start, goal, heuristic ?? new MapGridPathSettings().Heuristic);
    }

    public Map GetMap(string mapName) => _maps[mapName];
    public bool TryGetMap(string mapName, out Map map) => _maps.TryGetValue(mapName, out map!);

    private readonly GameData _data;
    private readonly Dictionary<string, Map> _maps;
    private readonly MapGraph _mapsGraph;
}
