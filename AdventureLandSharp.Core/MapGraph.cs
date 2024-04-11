using System.Numerics;

namespace AdventureLandSharp.Core;

public interface IMapGraphEdge {
    public MapLocation Source { get; }
    public MapLocation Dest { get; }
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
}

public readonly record struct MapGraphEdgeIntraMap(MapLocation Source, MapLocation Dest, List<Vector2> Path, float Cost) : IMapGraphEdge {
    public override readonly string ToString() => $"Traversing {Source.Map.Name} from {Source.Location} to {Dest.Location} with cost {Cost}.";
}

public readonly record struct MapGraphEdgeTeleport(MapLocation Source, MapLocation Dest) : IMapGraphEdge {
    public override readonly string ToString() => $"Teleporting from {Source} to {Dest}.";
}

public class MapGraph {
    public IEnumerable<MapLocation> Vertices => _vertices;
    public IReadOnlyDictionary<MapLocation, HashSet<IMapGraphEdge>> Edges => _edges;

    public MapGraph(IReadOnlyDictionary<string, Map> maps) {
        // Add a vertex for every connection point between areas, and add an edge between each connection.
        foreach (MapConnection connection in maps.SelectMany(x => x.Value.Connections)) {
            MapLocation from = new(maps[connection.SourceMap], new(connection.SourceX, connection.SourceY));
            MapLocation to = new(maps[connection.DestMap], new(connection.DestX, connection.DestY));

            AddVertex(from);
            AddVertex(to);

            AddEdge(new MapGraphEdgeInterMap(from, to, connection.DestSpawnId, connection.Type));
        }

        // Add a vertex for each default spawn (teleport location) for each area.
        foreach (Map map in maps.Values) {
            AddVertex(map.DefaultSpawn);
        }

        ThreadLocal<List<IMapGraphEdge>> edges = new(() => [], trackAllValues: true);

        // Compute edges between all vertices in the same area.
        Parallel.ForEach(_vertices.GroupBy(x => x.Map).Select(x => x.ToList()), vertsInMap => {
            Map map = vertsInMap[0].Map;

            Parallel.For(0, vertsInMap.Count, i => {
                List<IMapGraphEdge> storage = edges.Value!;
                
                for (int j = i + 1; j < vertsInMap.Count; ++j) {
                    MapLocation from = vertsInMap[i];
                    MapLocation to = vertsInMap[j];

                    storage.AddRange([
                        ..map.FindPath(from.Location, to.Location),
                        ..map.FindPath(to.Location, from.Location)
                    ]);
                }
            });
        });

        foreach (IMapGraphEdge edge in edges.Values.SelectMany(x => x)) {
            AddEdge(edge);
        }
    }

    public List<IMapGraphEdge> InterMap_Djikstra(MapLocation start, MapLocation goal, MapGridHeuristic heuristic) {
        PriorityQueue<MapLocation, float> Q = new();
        Dictionary<MapLocation, float> dist = [];
        Dictionary<MapLocation, IMapGraphEdge> prev = [];

        IEnumerable<IMapGraphEdge> startToGoal = start.Map == goal.Map ?
            start.Map.FindPath(start.Location, goal.Location, heuristic) :
            [];

        IEnumerable<IMapGraphEdge> startToFirstVertex = _vertices
            .AsParallel()
            .OrderByDescending(x => Vector2.Distance(start.Location, x.Location))
            .Where(x => x.Map == start.Map)
            .Select(x => x.Map.FindPath(start.Location, x.Location, heuristic))
            .Where(x => x.Any())
            .SelectMany(x => x);

        IEnumerable<IMapGraphEdge> lastVertexToGoal = _vertices
            .AsParallel()
            .OrderByDescending(x => Vector2.Distance(x.Location, goal.Location))
            .Where(x => x.Map == goal.Map)
            .Select(x => x.Map.FindPath(x.Location, goal.Location, heuristic))
            .Where(x => x.Any())
            .SelectMany(x => x);

        dist[start] = 0;
        Q.Enqueue(start, 0);

        while (Q.TryDequeue(out MapLocation u, out float _)) {
            IEnumerable<IMapGraphEdge> neighbors = _edges.TryGetValue(u, out HashSet<IMapGraphEdge>? edges) ? edges : [];

            if (start.Map == goal.Map) {
                neighbors = neighbors.Concat(startToGoal);
            }

            if (u == start) {
                neighbors = neighbors.Concat(startToFirstVertex);
            }

            if (u.Map == goal.Map) {
                neighbors = neighbors.Concat(lastVertexToGoal).Where(x => x.Source == u);
            }

            foreach (IMapGraphEdge edge in neighbors) {
                MapLocation vLoc = edge.Dest;
                float alt = 1 + dist[u] + (edge is MapGraphEdgeIntraMap intraMapEdge ? intraMapEdge.Cost : 1);

                if (!dist.TryGetValue(vLoc, out float prevAlt) || alt < prevAlt) {
                    prev[vLoc] = edge;
                    dist[vLoc] = alt;
                    Q.Enqueue(vLoc, alt);
                }
            }
        }

        if (dist.TryGetValue(goal, out float _)) {
            List<IMapGraphEdge> path = [];
            MapLocation current = goal;

            while (prev.TryGetValue(current, out IMapGraphEdge? edge)) {
                path.Add(edge);
                current = edge.Source;
            }

            path.Reverse();
            return path;
        }

        return [];
    }

    private readonly HashSet<MapLocation> _vertices = [];
    private readonly Dictionary<MapLocation, HashSet<IMapGraphEdge>> _edges = [];

    private void AddEdge(IMapGraphEdge edge) {
        _edges.TryAdd(edge.Source, new HashSet<IMapGraphEdge>(new MapGraphEdgeComparer()));
        _edges[edge.Source].Add(edge);
    }

    private void AddVertex(MapLocation vertex) {
        _vertices.Add(vertex);
    }
}