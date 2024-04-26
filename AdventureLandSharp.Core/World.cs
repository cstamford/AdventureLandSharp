using System.Diagnostics;
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
    public MapGraph MapsGraph => _mapsGraph;

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public IEnumerable<IMapGraphEdge> FindRoute(MapLocation start, MapLocation goal, MapGridPathSettings? settings = null) {
        settings ??= new();
        List<IMapGraphEdge> edges = _mapsGraph.InterMap_Djikstra(start, goal, settings.Value);

        // Attempt to merge any adjacent intra-map edges in the same map via a direct path.
        // This can increase the accuracy (avoiding useless ramp on/off), but we limit by cost to ensure we don't have a large perf hit.
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
                    mergeStart.Location,
                    mergeGoal.Location,
                    settings!.Value with { MaxCost = 100 });

                if (mergedEdge != null) {
                    edges[i] = mergedEdge;
                    edges.RemoveRange(i + 1, oneAfterMergeIdx - i - 1);
                }
            }
        }

        return edges;
    }

    public Map GetMap(string mapName) => _maps[mapName];
    public bool TryGetMap(string mapName, out Map map) => _maps.TryGetValue(mapName, out map!);

    private readonly GameData _data;
    private readonly Dictionary<string, Map> _maps;
    private readonly MapGraph _mapsGraph;
}
