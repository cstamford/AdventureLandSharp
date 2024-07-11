using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace AdventureLandSharp.Core;

public class World {
    public World(GameData data, Dictionary<string, GameDataSmap> smapData) {
        _data = data;
        _maps = data.Maps
            .Select(x => 
                data.Maps.TryGetValue(x.Key, out GameDataMap map) && 
                data.Geometry.TryGetValue(x.Key, out GameLevelGeometry geo) ? 
                    new Map(x.Key, data, map, geo, smapData.GetValueOrDefault(x.Key)) : 
                    null)
            .Where(x => x != null)
            .ToDictionary(k => k!.Name, v => v!);

        _bank1Location = GetMap("bank").DefaultSpawn;
        _bank2Location = GetMap("bank_b").DefaultSpawn;
        _upgradeLocations = new(GetMap("main"), new(-204, -129));
        _exchangeLocations = new(GetMap("main"), new(-26, -432));
        _potionLocations = [
            new(GetMap("halloween"), new(149, -182)),
            new(GetMap("winter_inn"), new(-84, -173)),
            new(GetMap("main"), new(56, -122))
        ];
        _scrollsLocation = new(GetMap("main"), new(-465, -71));

        _gooBrawlLocation = GetMap("goobrawl").DefaultSpawn;
        _bigAssCrabLocation = new(GetMap("main"), new(-1000, 1700));
        _frankyLocation = new(GetMap("level2w"), new(-300, 150));
        _iceGolemLocation = new(GetMap("winterland"), new(820, 425));
        _abTestingLocation = GetMap("abtesting").DefaultSpawn;

        // Add these locations as additional vertices to the graph.
        _mapsGraph = new(_maps, extraVertices: [
            _bank1Location,
            _bank2Location,
            _upgradeLocations,
            _exchangeLocations,
            .._potionLocations,
            _scrollsLocation,
            _gooBrawlLocation,
            _bigAssCrabLocation,
            _frankyLocation,
            _iceGolemLocation,
            _abTestingLocation
        ]);
    }

    public GameData Data => _data;
    public IEnumerable<KeyValuePair<string, Map>> Maps => _maps;
    public MapGraph MapsGraph => _mapsGraph;

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public IEnumerable<IMapGraphEdge> FindRoute(MapLocation start, MapLocation goal, MapGraphPathSettings? graphSettings = null, MapGridPathSettings? gridSettings = null) {
        graphSettings ??= new();
        gridSettings ??= new();

        (MapLocation, MapLocation, MapGraphPathSettings, MapGridPathSettings) key = (start, goal, graphSettings.Value, gridSettings.Value);
        if (!_mapsGraphCache.TryGetValue(key, out List<IMapGraphEdge>? cachedEdges)) {
            cachedEdges = _mapsGraph.InterMap_Djikstra(start, goal, graphSettings.Value, gridSettings.Value);
            MergeAdjacentIntraMapEdgesInPlace(cachedEdges, gridSettings.Value);
            _mapsGraphCache.TryAdd(key, cachedEdges);
        }

        return cachedEdges;
    }

    public Map GetMap(string mapName) => _maps[mapName];
    public bool TryGetMap(string mapName, out Map map) => _maps.TryGetValue(mapName, out map!);

    public MapLocation BankLocationFloor1 => _bank1Location;
    public MapLocation BankLocationFloor2 => _bank2Location;
    public MapLocation UpgradeLocations => _upgradeLocations;
    public MapLocation ExchangeLocations => _exchangeLocations;
    public MapLocation[] PotionLocations => _potionLocations;
    public MapLocation ScrollsLocation => _scrollsLocation;

    public MapLocation GooBrawlLocation => _gooBrawlLocation;
    public MapLocation BigAssCrabLocation => _bigAssCrabLocation;
    public MapLocation FrankyLocation => _frankyLocation;
    public MapLocation IceGolemLocation => _iceGolemLocation;
    public MapLocation ABTestingLocation => _abTestingLocation;

    private readonly GameData _data;
    private readonly Dictionary<string, Map> _maps;
    private readonly MapGraph _mapsGraph;
    private readonly ConcurrentDictionary<(MapLocation, MapLocation, MapGraphPathSettings, MapGridPathSettings), List<IMapGraphEdge>> _mapsGraphCache = [];

    private readonly MapLocation _bank1Location;
    private readonly MapLocation _bank2Location;
    private readonly MapLocation _upgradeLocations;
    private readonly MapLocation _exchangeLocations;
    private readonly MapLocation[] _potionLocations;
    private readonly MapLocation _scrollsLocation;

    private readonly MapLocation _gooBrawlLocation;
    private readonly MapLocation _bigAssCrabLocation;
    private readonly MapLocation _frankyLocation;
    private readonly MapLocation _iceGolemLocation;
    private readonly MapLocation _abTestingLocation;

    // Attempt to merge any adjacent intra-map edges in the same map via a direct path.
    // This can increase the accuracy (avoiding useless ramp on/off), but we limit by cost to ensure we don't have a large perf hit.
    private static void MergeAdjacentIntraMapEdgesInPlace(List<IMapGraphEdge> edges, MapGridPathSettings settings) {
        for (int i = 0; i < edges.Count; ++i) {
            if (edges[i] is not MapGraphEdgeIntraMap edge) {
                continue;
            }

            int oneAfterMergeIdx = i + 1;

            while (oneAfterMergeIdx < edges.Count && 
                edges[oneAfterMergeIdx] is MapGraphEdgeIntraMap nextEdge && 
                nextEdge.Dest.Map == edge.Source.Map)
            {
                ++oneAfterMergeIdx;
            }

            if (oneAfterMergeIdx != i + 1) {
                MapLocation mergeStart = edge.Source;
                MapLocation mergeGoal = edges[oneAfterMergeIdx - 1].Dest;
                Debug.Assert(mergeStart.Map == mergeGoal.Map);

                MapGraphEdgeIntraMap? mergedEdge = mergeStart.Map.FindPath(
                    mergeStart.Position,
                    mergeGoal.Position,
                    settings with { MaxCost = 128 }
                );

                if (mergedEdge != null) {
                    edges[i] = mergedEdge;
                    edges.RemoveRange(i + 1, oneAfterMergeIdx - i - 1);
                }
            }
        }
    }
}
