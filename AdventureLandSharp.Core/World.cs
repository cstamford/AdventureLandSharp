namespace AdventureLandSharp.Core;

public class World {
    public World(ref readonly GameData data) {
        _data = data;
        _maps = data.Maps
            .Select<KeyValuePair<string, GameDataMap>, (string, GameDataMap, GameLevelGeometry)?>(x =>
                _data.Maps.TryGetValue(x.Key, out GameDataMap map) &&
                _data.Geometry.TryGetValue(x.Key, out GameLevelGeometry geo)
                    ? (x.Key, map, geo)
                    : null)
            .Where(x => x.HasValue)
            .Select(x => {
                GameDataMap mapData = x!.Value.Item2;
                GameLevelGeometry mapGeometry = x!.Value.Item3;
                return new Map(x!.Value.Item1, in _data, in mapData, in mapGeometry);
            })
            .ToDictionary(map => map.Name);

        MapsGraph = new(_maps);
    }

    public ref readonly GameData Data => ref _data;

    public IEnumerable<KeyValuePair<string, Map>> Maps => _maps;
    public MapGraph MapsGraph { get; }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public IEnumerable<IMapGraphEdge> FindRoute(MapLocation start, MapLocation goal, MapGridHeuristic? heuristic = null) =>
        MapsGraph.InterMap_Djikstra(start, goal, heuristic ?? new MapGridPathSettings().Heuristic);

    public Map GetMap(string mapName) => _maps[mapName];

    public bool TryGetMap(string mapName, out Map map) => _maps.TryGetValue(mapName, out map!);
    private readonly GameData _data;
    private readonly Dictionary<string, Map> _maps;
}
