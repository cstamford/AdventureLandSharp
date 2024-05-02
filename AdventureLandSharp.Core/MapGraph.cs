using System.Collections.Concurrent;
using System.Diagnostics;
using System.Numerics;
using AdventureLandSharp.Core.Util;

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
    public float Cost => 1;
}

public readonly record struct MapGraphEdgeIntraMap(MapLocation Source, MapLocation Dest, List<Vector2> Path, float Cost) : IMapGraphEdge {
    public override readonly string ToString() => $"Traversing {Source.Map.Name} from {Source.Location} to {Dest.Location} with cost {Cost}.";
}

public readonly record struct MapGraphEdgeTeleport(MapLocation Source, MapLocation Dest) : IMapGraphEdge {
    public override readonly string ToString() => $"Teleporting from {Source} to {Dest}.";
    public float Cost => 50;
}

public class MapGraph {
    public IEnumerable<MapLocation> Vertices => _vertices;
    public IReadOnlyDictionary<MapLocation, HashSet<IMapGraphEdge>> Edges => _edges;

    public MapGraph(IReadOnlyDictionary<string, Map> maps) {
        foreach (Map map in maps.Values) {
            // Creating a vertex for every spawn location on the map, including teleport.
            foreach (double[] spawn in map.Data.SpawnPositions) {
                AddVertex(new(map, new((float)spawn[0], (float)spawn[1])));
            }

            // Creating a vertex for every monster spawn location on the map, as they are common destinations.
            foreach (GameDataMapMonster monster in map.Data.Monsters ?? []) {
                foreach ((string? spawnMapName, Vector2 spawnLoc) in monster.GetSpawnLocations()) {
                    AddVertex(new(spawnMapName == null ? map : maps[spawnMapName], spawnLoc));
                }
            }

            // Creating a vertex for every NPC, for similar reason to monsters.
            foreach (GameDataMapNpc npc in map.Data.Npcs ?? []) {
                Vector2? pos = npc.GetPosition();
                if (pos != null) {
                    AddVertex(new(map, pos.Value));
                }
            }

            // Adding vertices for each connection point and creating edges between them.
            foreach (MapConnection connection in map.Connections) {
                MapLocation from = new(maps[connection.SourceMap], new(connection.SourceX, connection.SourceY));
                AddVertex(from);

                MapLocation to = new(maps[connection.DestMap], new(connection.DestX, connection.DestY));
                AddVertex(to);

                AddEdge(new MapGraphEdgeInterMap(from, to, connection.DestSpawnId, connection.Type));
            }
        }

        foreach (IGrouping<Map, MapLocation> vertices in _vertices.GroupBy(x => x.Map)) {
            Map map = vertices.Key;
            ConcurrentBag<IMapGraphEdge> edges = [];

            // Creating an edge between each pair of vertices.
            Parallel.ForEach(vertices, u => {
                Parallel.ForEach(vertices.Where(v => v != u && v != v.Map.DefaultSpawn), v => {
                    MapGraphEdgeIntraMap? edge = map.FindPath(u.Location, v.Location);
                    if (edge?.Cost > 0) {
                        edges.Add(edge!);
                    }
                });
            });

            foreach (IMapGraphEdge edge in edges) {
                AddEdge(edge);
            }
        };
    }

    public List<IMapGraphEdge> InterMap_Djikstra(MapLocation start, MapLocation goal, MapGridPathSettings settings) {
        PriorityQueue<MapLocation, float> Q = new();
        Dictionary<MapLocation, float> dist = [];
        Dictionary<MapLocation, IMapGraphEdge> prev = [];

        // Get the nearest vertex to the start, preferring one that is towards the goal.
        // This prevents bouncing backwards if the nearest vertex is behind us, and then forwards again.
        IEnumerable<MapLocation> rampOnCandidates = _vertices
            .Where(x => x.Map == start.Map)
            .OrderBy(x => start.Location.SimpleDist(x.Location));

        // Fall back to the closest vertex if we can't find one that is towards the goal.
        MapLocation rampOn = rampOnCandidates
            .FirstOrNull(x => Vector2.Dot(start.Location - x.Location, start.Location - goal.Location) > 0) 
            ?? rampOnCandidates.First();

        // Get the nearest vertex to the end.
        MapLocation rampOff = _vertices
            .Where(x => x.Map == goal.Map)
            .OrderBy(x => goal.Location.SimpleDist(x.Location))
            .First();

        MapGraphEdgeIntraMap? directPath = null;
        bool needsRamp = true;

        if (start.Map == goal.Map) {
            // In the event that this is a direct path (same map), try generating a path directly.
            // This will prevent us from bouncing between vertices.
            // Note that we still want to run Dijkstra's to look for cool shortcuts.
            directPath = start.Map.FindPath(start.Location, goal.Location, settings with { MaxCost = 512 });

            // Only trigger ramp generation if using the ramp is likely to be cheaper than the direct path.
            // This is because if it isn't, it's very unlikely to be useful, and costs a lot of time.
            // There are niche cases where it is useful, but they are few and far between.
            float estimatedDistanceForRamp = start.Location.SimpleDist(rampOn.Location) + rampOn.Location.SimpleDist(goal.Location);
            needsRamp = !directPath.HasValue || estimatedDistanceForRamp < directPath.Value.Cost * MapGrid.CellSize;
        }

        MapGraphEdgeIntraMap? startToRampOn = null;
        MapGraphEdgeIntraMap? rampOffToGoal = null;

        if (needsRamp) {
            // We generate a path from start to rampOn, and from rampOff to goal.
            // Note that they will be null if these vertices are the same as the start or goal (e.g. already in graph).
            startToRampOn = start.Map.FindPath(start.Location, rampOn.Location, settings);
            rampOffToGoal = goal.Map.FindPath(rampOff.Location, goal.Location, settings);
        }

        Debug.Assert(directPath.HasValue || startToRampOn.HasValue || _edges.ContainsKey(start), "No connection to the start.");
        Debug.Assert(directPath.HasValue || rampOffToGoal.HasValue || _edges.ContainsKey(goal), "No connection to the goal.");

        dist[start] = 0;
        Q.Enqueue(start, 0);

        while (Q.TryDequeue(out MapLocation u, out float _) && u != goal) {
            IEnumerable<IMapGraphEdge?> specialEdges = [
                u == start ? startToRampOn : null,
                u == start ? directPath : null,
                u == rampOff ? rampOffToGoal : null,
                new MapGraphEdgeTeleport(u, u.Map.DefaultSpawn) // note: we can always teleport back to the default spawn
            ];

            foreach (IMapGraphEdge? edge in specialEdges.Where(x => x != null)) {
                VisitEdge(edge!);
            }

            foreach (IMapGraphEdge edge in _edges.TryGetValue(u, out HashSet<IMapGraphEdge>? edges) ? edges : []) {
                VisitEdge(edge);
            }

            void VisitEdge(IMapGraphEdge edge) {
                MapLocation vLoc = edge.Dest;
                float alt = 1 + dist[u] + edge.Cost;

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