namespace AdventureLandSharp.Core;

public interface IMapGraphEdge
{
    public MapLocation Source { get; }
    public MapLocation Dest { get; }
}

internal class MapGraphEdgeComparer : IEqualityComparer<IMapGraphEdge>
{
    public bool Equals(IMapGraphEdge? lhs, IMapGraphEdge? rhs)
    {
        return lhs != null && rhs != null &&
               lhs.Source.CompareTo(rhs.Source) == 0 &&
               lhs.Dest.CompareTo(rhs.Dest) == 0;
    }

    public int GetHashCode(IMapGraphEdge e)
    {
        return HashCode.Combine(e.Source, e.Dest);
    }
}

public readonly record struct MapGraphEdgeInterMap(
    MapLocation Source,
    MapLocation Dest,
    long DestSpawnId,
    MapConnectionType Type) : IMapGraphEdge
{
    public readonly override string ToString()
    {
        return $"{Type} from {Source} to {Dest}.";
    }
}

public readonly record struct MapGraphEdgeIntraMap(MapLocation Source, MapLocation Dest, List<Vector2> Path, float Cost)
    : IMapGraphEdge
{
    public readonly override string ToString()
    {
        return $"Traversing {Source.Map.Name} from {Source.Location} to {Dest.Location} with cost {Cost}.";
    }
}

public readonly record struct MapGraphEdgeTeleport(MapLocation Source, MapLocation Dest) : IMapGraphEdge
{
    public readonly override string ToString()
    {
        return $"Teleporting from {Source} to {Dest}.";
    }
}

public class MapGraph
{
    private readonly Dictionary<MapLocation, HashSet<IMapGraphEdge>> _edges = [];

    private readonly HashSet<MapLocation> _vertices = [];

    public MapGraph(IReadOnlyDictionary<string, Map> maps)
    {
        // Add a vertex for every connection point between areas, and add an edge between each connection.
        foreach (var connection in maps.SelectMany(x => x.Value.Connections))
        {
            MapLocation from = new(maps[connection.SourceMap], new Vector2(connection.SourceX, connection.SourceY));
            MapLocation to = new(maps[connection.DestMap], new Vector2(connection.DestX, connection.DestY));

            AddVertex(from);
            AddVertex(to);

            AddEdge(new MapGraphEdgeInterMap(from, to, connection.DestSpawnId, connection.Type));
        }

        // Add a vertex for each default spawn (teleport location) for each area.
        foreach (var map in maps.Values) AddVertex(map.DefaultSpawn);

        ThreadLocal<List<IMapGraphEdge>> edges = new(() => [], true);

        // Compute edges between all vertices in the same area.
        Parallel.ForEach(_vertices.GroupBy(x => x.Map).Select(x => x.ToList()), vertsInMap =>
        {
            var map = vertsInMap[0].Map;

            Parallel.For(0, vertsInMap.Count, i =>
            {
                var storage = edges.Value!;

                for (var j = i + 1; j < vertsInMap.Count; ++j)
                {
                    var from = vertsInMap[i];
                    var to = vertsInMap[j];

                    storage.AddRange([
                        ..map.FindPath(from.Location, to.Location),
                        ..map.FindPath(to.Location, from.Location)
                    ]);
                }
            });
        });

        foreach (var edge in edges.Values.SelectMany(x => x)) AddEdge(edge);
    }

    public IEnumerable<MapLocation> Vertices => _vertices;
    public IReadOnlyDictionary<MapLocation, HashSet<IMapGraphEdge>> Edges => _edges;

    public List<IMapGraphEdge> InterMap_Djikstra(MapLocation start, MapLocation goal, MapGridHeuristic heuristic)
    {
        PriorityQueue<MapLocation, float> queue = new();
        Dictionary<MapLocation, float> dist = [];
        Dictionary<MapLocation, IMapGraphEdge> prev = [];

        var startToGoal = start.Map == goal.Map ? start.Map.FindPath(start.Location, goal.Location, heuristic) : [];

        var startToFirstVertex = _vertices
            .AsParallel()
            .OrderByDescending(x => Vector2.Distance(start.Location, x.Location))
            .Where(x => x.Map == start.Map)
            .Select(x => x.Map.FindPath(start.Location, x.Location, heuristic))
            .Where(x => x.Length != 0)
            .SelectMany(x => x)
            .ToArray();

        var lastVertexToGoal = _vertices
            .AsParallel()
            .OrderByDescending(x => Vector2.Distance(x.Location, goal.Location))
            .Where(x => x.Map == goal.Map)
            .Select(x => x.Map.FindPath(x.Location, goal.Location, heuristic))
            .Where(x => x.Length != 0)
            .SelectMany(x => x)
            .ToArray();

        dist[start] = 0;
        queue.Enqueue(start, 0);

        while (queue.TryDequeue(out var u, out var _))
        {
            IEnumerable<IMapGraphEdge> neighbors = _edges.TryGetValue(u, out var edges) ? edges : [];

            if (start.Map == goal.Map) neighbors = neighbors.Concat(startToGoal);

            if (u == start) neighbors = neighbors.Concat(startToFirstVertex);

            if (u.Map == goal.Map) neighbors = neighbors.Concat(lastVertexToGoal).Where(x => x.Source == u);

            foreach (var edge in neighbors)
            {
                var vLoc = edge.Dest;
                var alt = 1 + dist[u] + (edge is MapGraphEdgeIntraMap intraMapEdge ? intraMapEdge.Cost : 1);

                if (dist.TryGetValue(vLoc, out var prevAlt) && !(alt < prevAlt)) continue;

                prev[vLoc] = edge;
                dist[vLoc] = alt;
                queue.Enqueue(vLoc, alt);
            }
        }

        if (!dist.TryGetValue(goal, out var _)) return [];

        List<IMapGraphEdge> path = [];
        var current = goal;

        while (prev.TryGetValue(current, out var edge))
        {
            path.Add(edge);
            current = edge.Source;
        }

        path.Reverse();
        return path;
    }

    private void AddEdge(IMapGraphEdge edge)
    {
        _edges.TryAdd(edge.Source, new HashSet<IMapGraphEdge>(new MapGraphEdgeComparer()));
        _edges[edge.Source].Add(edge);
    }

    private void AddVertex(MapLocation vertex)
    {
        _vertices.Add(vertex);
    }
}