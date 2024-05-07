using System.Collections.Concurrent;
using System.Numerics;
using AdventureLandSharp.Core.Util;
using Faster.Map.QuadMap;

namespace AdventureLandSharp.Core;

public interface IMapGraphEdge {
    public MapLocation Source { get; }
    public MapLocation Dest { get; }
    public float Cost { get; }
}

class MapGraphEdgeComparer : IEqualityComparer<IMapGraphEdge> {
    public bool Equals(IMapGraphEdge? lhs, IMapGraphEdge? rhs) =>
        lhs != null && rhs != null &&
        lhs.Source.CompareTo(rhs.Source) == 0 &&
        lhs.Dest.CompareTo(rhs.Dest) == 0;

    public int GetHashCode(IMapGraphEdge e) => HashCode.Combine(e.Source, e.Dest);
}

public readonly record struct MapGraphEdgeInterMap(MapLocation Source, MapLocation Dest, long DestSpawnId, MapConnectionType Type) : IMapGraphEdge {
    public override readonly string ToString() => $"{Type} from {Source} to {Dest}.";
    public float Cost => 50;
}

public readonly record struct MapGraphEdgeIntraMap(MapLocation Source, MapLocation Dest, List<Vector2> Path, float Cost) : IMapGraphEdge {
    public override readonly string ToString() => $"Traversing {Source.Map.Name} from {Source.Position} to {Dest.Position} with cost {Cost}.";
}

public readonly record struct MapGraphEdgeTeleport(MapLocation Source, MapLocation Dest) : IMapGraphEdge {
    public override readonly string ToString() => $"Teleporting from {Source} to {Dest}.";
    public float Cost => 100;
}

public readonly record struct MapGraphEdgeJoin(MapLocation Source, MapLocation Dest, string EventName) : IMapGraphEdge {
    public override readonly string ToString() => $"Joining from {Source} to {Dest} for {EventName}.";
    public float Cost => 50;
}

public readonly record struct MapGraphPathSettings(bool EnableTeleport, List<(string EventName, MapLocation Dest)> EnableEvents) {
    public MapGraphPathSettings() : this(EnableTeleport: true, EnableEvents: []) { }
}

public class MapGraph {
    public MapGraph(IReadOnlyDictionary<string, Map> maps) {
        _mapStorage = maps.ToDictionary(x => x.Value, x => new MapStorage(x.Value));

        foreach ((Map map, MapStorage storage) in _mapStorage) {
            // Creating a vertex for every spawn location on the map, including teleport.
            foreach (double[] spawn in map.Data.SpawnPositions) {
                storage.Vertices.Add(new((float)spawn[0], (float)spawn[1]));
            }

            // Creating a vertex for every monster spawn location on the map.
            foreach (GameDataMapMonster monster in map.Data.Monsters ?? []) {
                foreach ((string? spawnMapName, Vector2 spawnPos) in monster.GetSpawnLocations()) {
                    MapStorage monsterMapStorage = spawnMapName == null ? storage : _mapStorage[maps[spawnMapName]];
                    monsterMapStorage.Vertices.Add(spawnPos);
                }
            }
        }

        // Adding vertices for each connection point.
        foreach (MapConnection connection in maps.SelectMany(x => x.Value.Connections)) {
            MapLocation from = new(maps[connection.SourceMap], new(connection.SourceX, connection.SourceY));
            _mapStorage[from.Map].Vertices.Add(from.Position);

            MapLocation to = new(maps[connection.DestMap], new(connection.DestX, connection.DestY));
            _mapStorage[to.Map].Vertices.Add(to.Position);
        }
    }

    public List<IMapGraphEdge> InterMap_Djikstra(
        MapLocation start,
        MapLocation goal,
        MapGraphPathSettings graphSettings,
        MapGridPathSettings gridSettings)
    {
        // Get the nearest vertex to the start, preferring one that is towards the goal.
        // This prevents bouncing backwards if the nearest vertex is behind us, and then forwards again.
        IEnumerable<Vector2> rampOnCandidates = _mapStorage[start.Map].Vertices.OrderBy(x => start.Position.SimpleDist(x));

        // Fall back to the closest vertex if we can't find one that is towards the goal.
        Vector2 rampOnPosition = rampOnCandidates.FirstOrNull(x => 
            start.Map == goal.Map && Vector2.Dot(start.Position - x, start.Position - goal.Position) > 0) ?? rampOnCandidates.First();

        // Get the nearest vertex to the end.
        Vector2 rampOffPosition = _mapStorage[goal.Map].Vertices.OrderBy(x => goal.Position.SimpleDist(x)).First();

        MapGraphEdgeIntraMap? directPath = null;
        bool needsRamp = true;

        if (start.Map == goal.Map) {
            // In the event that this is a direct path (same map), try generating a path directly.
            // This will prevent us from bouncing between vertices.
            // Note that we still want to run Dijkstra's to look for cool shortcuts.
            directPath = start.Map.FindPath(start.Position, goal.Position, gridSettings with { MaxCost = 512 });

            // Only trigger ramp generation if using the ramp is likely to be cheaper than the direct path.
            // This is because if it isn't, it's very unlikely to be useful, and costs a lot of time.
            // There are niche cases where it is useful, but they are few and far between.
            float estimatedDistanceForRamp = start.Position.SimpleDist(rampOnPosition) + rampOnPosition.SimpleDist(goal.Position);
            needsRamp = !directPath.HasValue || estimatedDistanceForRamp < directPath.Value.Cost * MapGrid.CellSize;
        }

        MapGraphEdgeIntraMap? startToRampOn = null;
        MapGraphEdgeIntraMap? rampOffToGoal = null;

        if (needsRamp) {
            // We generate a path from start to rampOnPosition, and from rampOffPosition to goal.
            // Note that they will be null if these vertices are the same as the start or goal (e.g. already in graph).
            startToRampOn = start.Map.FindPath(start.Position, rampOnPosition, gridSettings);
            rampOffToGoal = goal.Map.FindPath(rampOffPosition, goal.Position, gridSettings);
        }

        if (!directPath.HasValue && !startToRampOn.HasValue && FetchEdges(start).Count == 0) {
            throw new Exception(
                "No connection from start to the graph. " +
                $"Start:{start}, Goal:{goal}, startToRampOn:{startToRampOn}, GraphSettings:{graphSettings}, GridSettings:{gridSettings}");
        }

        if (!directPath.HasValue && !rampOffToGoal.HasValue && FetchEdges(goal).Count == 0) {
            throw new Exception(
                "No connection from the graph to the goal. " +
                $"Start:{start}, Goal:{goal}, rampOffToGoal:{rampOffToGoal}, GraphSettings:{graphSettings}, GridSettings:{gridSettings}");
        }

        QuadMap<MapLocation, (float Alt, IMapGraphEdge? Edge)> dict = _dictPool.Value!;
        FastPriorityQueue<MapLocation> Q = _queuePool.Value!;

        dict.Clear();
        dict.Emplace(start, (0, null));

        Q.Clear();
        Q.Enqueue(start, 0);

        while (Q.TryDequeue(out MapLocation u, out float _) && u != goal) {
            float prevAlt = dict[u].Alt;

            if (u == startToRampOn?.Source) {
                VisitEdge(startToRampOn);
            }

            if (u == directPath?.Source) {
                VisitEdge(directPath);
            }

            if (u == rampOffToGoal?.Source) {
                VisitEdge(rampOffToGoal);
            }

            foreach (IMapGraphEdge edge in FetchEdges(u)) {
                VisitEdge(edge);
            }

            if (graphSettings.EnableTeleport) {
                VisitEdge(new MapGraphEdgeTeleport(u, u.Map.DefaultSpawn));
            }

            foreach ((string EventName, MapLocation Dest) in graphSettings.EnableEvents) {
                VisitEdge(new MapGraphEdgeJoin(u, Dest, EventName));
            }

            void VisitEdge(IMapGraphEdge edge) {
                bool exists = dict.Get(edge.Dest, out (float Alt, IMapGraphEdge? Cell) cur);
                float alt = prevAlt + edge.Cost;

                if (!exists) {
                    dict.Emplace(edge.Dest, (alt, edge));
                } else if (alt < cur.Alt) {
                    dict.Update(edge.Dest, (alt, edge));
                } else {
                    return;
                }

                Q.Enqueue(edge.Dest, alt);

                // Kick off the generation of the next edges.
                _ = Task.Run(() => FetchEdges(edge.Dest));
            }
        }

        if (dict.Get(goal, out _)) {
            List<IMapGraphEdge> path = [];

            MapLocation current = goal;
            while (dict.Get(current, out (float _, IMapGraphEdge? Edge) cur) && cur.Edge != null) {
                path.Add(cur.Edge);
                current = cur.Edge.Source;
            }

            path.Reverse();
            return path;
        }

        return [];
    }

    private class MapStorage(Map map) {
        public override string ToString() => $"{map.Name} {Vertices.Count}v {Edges.Count}e";
        public Map Map => map;
        public HashSet<Vector2> Vertices { get; } = [];
        public ConcurrentDictionary<Vector2, MapStorageEdges> Edges { get; } = [];
    };

    public class MapStorageEdges() {
        public int Generating;
        public bool Generated;
        public HashSet<IMapGraphEdge> Data = new(new MapGraphEdgeComparer());
    }

    private readonly Dictionary<Map, MapStorage> _mapStorage = [];
    private static readonly ThreadLocal<QuadMap<MapLocation, (float Alt, IMapGraphEdge Edge)>> _dictPool = new(() => new());
    private static readonly ThreadLocal<FastPriorityQueue<MapLocation>> _queuePool = new(() => new());

    private HashSet<IMapGraphEdge> FetchEdges(MapLocation u) {
        MapStorage storage = _mapStorage[u.Map];

        // If we can't find this vertex in the graph, then we can't find any edges. It's the start or goal.
        if (!storage.Vertices.Contains(u.Position)) {
            return [];
        }

        // Check if they've been generated, or are currently being generated.
        // If so, we just wait and return when they're done.
        MapStorageEdges edges = storage.Edges.GetOrAdd(u.Position, (_) => new());

        if (edges.Generated || Interlocked.Exchange(ref edges.Generating, 1) == 1) {
            SpinWait.SpinUntil(() => edges.Generated);
            return edges.Data;
        }

        // The edges don't exist yet, and we are the first to generate them. Let's do it.
        ConcurrentBag<IMapGraphEdge> newEdges = [];

        // Creating an edge between each pair of vertices.
        Parallel.ForEach(storage.Vertices.Where(v => v != u.Position && v != u.Map.DefaultSpawn.Position), v => {
            MapGraphEdgeIntraMap? edge = u.Map.FindPath(u.Position, v);
            if (edge?.Cost > 0) {
                newEdges.Add(edge);
            }
        });

        // Creating an edge for any connections in this map which are a source from this vertex.
        foreach (MapConnection connection in u.Map.Connections.Where(x => x.SourceX == u.Position.X && x.SourceY == u.Position.Y)) {
            MapLocation to = new(_mapStorage.First(x => x.Key.Name == connection.DestMap).Key, new(connection.DestX, connection.DestY));
            MapGraphEdgeInterMap connectionEdge = new(u, to, connection.DestSpawnId, connection.Type);
            newEdges.Add(connectionEdge);
        }

        // Finally, merge the data in, and flag it as generated, which will now unblock any other threads.
        edges.Data.UnionWith(newEdges);
        edges.Generated = true;

        return edges.Data;
    }
}
