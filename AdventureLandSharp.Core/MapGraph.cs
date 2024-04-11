using System.Collections.Concurrent;
using System.Diagnostics;
using System.Numerics;

namespace AdventureLandSharp.Core;

public interface IMapGraphEdge {
    public MapLocation Source { get; }
    public MapLocation Dest { get; }
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
    public MapGraph(IReadOnlyDictionary<string, Map> maps) {
        foreach (MapConnection connection in maps.SelectMany(x => x.Value.Connections)) {
            Map fromMap = maps[connection.SourceMap];
            Map toMap = maps[connection.DestMap];

            MapLocation from = new(fromMap, fromMap.FindNearestWalkable(new Vector2(connection.SourceX, connection.SourceY)));
            MapLocation to = new(toMap, toMap.FindNearestWalkable(new Vector2(connection.DestX, connection.DestY)));

            _vertices.Add(from);
            _vertices.Add(to);

            _edges.TryAdd(from, []);
            _edges[from].Add(new MapGraphEdgeInterMap(from, to, connection.DestSpawnId, connection.Type));
        }

        ConcurrentBag<IMapGraphEdge> intraMapEdges = [];

        Parallel.ForEach(_vertices.GroupBy(x => x.Map).Select(x => x.ToList()), vertsInMap => {
            Parallel.For(0, vertsInMap.Count, i => {
                for (int j = i + 1; j < vertsInMap.Count; ++j) {
                    MapLocation from = vertsInMap[i];
                    MapLocation to = vertsInMap[j];

                    IEnumerable<IMapGraphEdge> edges =  [
                        ..to.Map.FindPath(to.Location, from.Location),
                        ..from.Map.FindPath(from.Location, to.Location)
                    ];

                    if (!edges.Any()) {
                        continue;
                    }

                    foreach (IMapGraphEdge edge in edges) {
                        intraMapEdges.Add(edge);
                    }
                }
            });
        });

        foreach (IMapGraphEdge edge in intraMapEdges) {
            _edges.TryAdd(edge.Source, []);
            _edges[edge.Source].Add(edge);
        }
    }

    public List<IMapGraphEdge> InterMap_Djikstra(MapLocation start, MapLocation goal, MapGridHeuristic heuristic) {
        Debug.Assert(start.Map != goal.Map, "InterMap_Djikstra requires start and goal to be on different maps.");

        PriorityQueue<MapLocation, float> Q = new();
        Dictionary<MapLocation, float> dist = [];
        Dictionary<MapLocation, IMapGraphEdge> prev = [];

        List<IMapGraphEdge> startToFirstVertex = [.._vertices
            .AsParallel()
            .OrderByDescending(x => Vector2.Distance(start.Location, x.Location))
            .Where(x => x.Map == start.Map)
            .Select(x => x.Map.FindPath(start.Location, x.Location, heuristic))
            .Where(x => x.Any())
            .SelectMany(x => x)];

        List<IMapGraphEdge> lastVertexToGoal = [.._vertices
            .AsParallel()
            .OrderByDescending(x => Vector2.Distance(x.Location, goal.Location))
            .Where(x => x.Map == goal.Map)
            .Select(x => x.Map.FindPath(x.Location, goal.Location, heuristic))
            .Where(x => x.Any())
            .SelectMany(x => x)];

        dist[start] = 0;
        Q.Enqueue(start, 0);

        while (Q.TryDequeue(out MapLocation u, out float _) && u != goal) {
            IEnumerable<IMapGraphEdge> neighbors = _edges.TryGetValue(u, out List<IMapGraphEdge>? edges) ? edges : [];

            if (u == start) {
                neighbors = neighbors.Concat(startToFirstVertex);
            } else if (u.Map == goal.Map) {
                neighbors = neighbors.Concat(lastVertexToGoal);
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
    private readonly Dictionary<MapLocation, List<IMapGraphEdge>> _edges = [];
}