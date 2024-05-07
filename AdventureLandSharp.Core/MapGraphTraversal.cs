using System.Diagnostics;
using System.Numerics;
using AdventureLandSharp.Core;
using AdventureLandSharp.Core.SocketApi;
using AdventureLandSharp.Core.Util;

namespace AdventureLandSharp;

public class MapGraphTraversal(Socket socket, IEnumerable<IMapGraphEdge> edges, MapLocation end) {
    public MapLocation End => end;
    public bool Finished => _edges.Count == 0 && (!CurrentEdgeValid || CurrentEdgeFinished);
    public IMapGraphEdge? CurrentEdge => _edge;

    public void Update() {
        DateTimeOffset now = DateTimeOffset.UtcNow;

        while (!Finished && (!CurrentEdgeValid || CurrentEdgeFinished)) {
            _log.Debug($"Progressing to next edge. CurrentEdge={CurrentEdge}, CurrentEdgeValid={CurrentEdgeValid}, CurrentEdgeFinished={CurrentEdgeFinished}.");
            _edge = _edges.Dequeue();
            _edgeUpdate = now;
            Player.MovementPlan = null;
        }

        if (Finished) {
            return;
        }

        Debug.Assert(CurrentEdgeValid);

        if (now >= _edgeUpdate) {
            ProcessEdge();
            _edgeUpdate = NextEdgeUpdate(now);
        }
    }

    private LocalPlayer Player => socket.Player;
    private readonly Queue<IMapGraphEdge> _edges = new(edges);
    private IMapGraphEdge? _edge;
    private DateTimeOffset _edgeUpdate;
    private readonly Logger _log = new(socket.Player.Name, "MapGraphTraversal");

    private bool CurrentEdgeFinished => _edge != null && _edge switch { 
        MapGraphEdgeInterMap or MapGraphEdgeJoin => 
            Player.MapName == _edge.Dest.Map.Name && Player.Position.Equivalent(_edge.Dest.Position),
        MapGraphEdgeIntraMap intraMap => 
            Player.Position.Equivalent(intraMap.Path[^1]) || Player.Position.Equivalent(intraMap.Dest.Position),
        MapGraphEdgeTeleport teleport => 
            Player.Position.Equivalent(teleport.Dest.Position, teleport.Dest.Map.DefaultSpawnScatter + MapGrid.CellWorldEpsilon),
        _ => true 
    };

    private bool CurrentEdgeValid => _edge != null && 
        (Player.MapName == _edge.Source.Map.Name || 
        (_edge is MapGraphEdgeInterMap && Player.MapName == _edge.Dest.Map.Name));

    private DateTimeOffset NextEdgeUpdate(DateTimeOffset now) => _edge switch {
        MapGraphEdgeInterMap or MapGraphEdgeJoin => now.Add(TimeSpan.FromSeconds(1.0)),
        MapGraphEdgeTeleport => now.Add(TimeSpan.FromSeconds(4.5)),
        _ => DateTimeOffset.MinValue
    };

    private void ProcessEdge() {
        if (_edge is MapGraphEdgeInterMap interMap) {
            _log.Debug($"{interMap}");
            if (interMap.Type is MapConnectionType.Door or MapConnectionType.Transporter) {
                socket.Emit<Outbound.Transport>(new(interMap.Dest.Map.Name, interMap.DestSpawnId));
            } else if (interMap.Type is MapConnectionType.Leave) {
                socket.Emit<Outbound.Leave>(new());
            } else {
                throw new NotImplementedException($"Unknown inter-map edge type: {interMap.Type}");
            }
        } else if (_edge is MapGraphEdgeJoin join) {
            _log.Debug($"{join}");
            socket.Emit<Outbound.Join>(new(join.EventName));
        } else if (_edge is MapGraphEdgeIntraMap intraMap) {
            if (Player.MovementPlan == null) {
                intraMap = ProcessEdge_MapTransitionDistanceSkip(intraMap);
                Player.MovementPlan = new ClickAheadMovementPlan(Player.Position, new(intraMap.Path), intraMap.Source.Map);
                _edge = intraMap;
            }
        } else if (_edge is MapGraphEdgeTeleport tp) {
            _log.Debug($"{tp}");
            socket.Emit<Outbound.Town>(new());
        } else {
            throw new NotImplementedException($"Unknown edge type: {_edge}");
        }
    }

    // Given an intra-map edge, trim the path to the next door or transporter based on the next edge.
    // This allows us to skip walking right to the end of the edge to use e.g. a door which has a useable distance.
    private MapGraphEdgeIntraMap ProcessEdge_MapTransitionDistanceSkip(MapGraphEdgeIntraMap edge) {
        if (_edges.TryPeek(out IMapGraphEdge? nextEdge) && nextEdge is MapGraphEdgeInterMap nextEdgeInter) {
            float buffer = Math.Max(GameConstants.PlayerWidth, GameConstants.PlayerHeight) + MapGrid.CellWorldEpsilon;
            float cuttableDistance = nextEdgeInter.Type switch {
                MapConnectionType.Door => GameConstants.DoorDist - buffer,
                MapConnectionType.Transporter => GameConstants.TransporterDist - buffer,
                _ => 0.0f
            };

            if (cuttableDistance > 0) {
                int cutIdx = edge.Path.FindIndex(x => x.SimpleDist(nextEdgeInter.Source.Position) < cuttableDistance);

                if (cutIdx != -1) {
                    int cutIdxOneAfter = cutIdx + 1;
                    int cutLength = edge.Path.Count - cutIdxOneAfter;

                    if (cutLength > 0) {
                        edge.Path.RemoveRange(cutIdxOneAfter, cutLength);
                    }
                }

                Vector2 newLocation = edge.Path.Count > 0 ? edge.Path[^1] : Player.Position;
                return edge with { Dest = edge.Dest with { Position = newLocation } };
            }
        }

        return edge;
    }


    // Given an intra-map edge, skips to the first node from the end that has line of sight to the player.
    // This allows us to avoid useless steps (e.g. go backwards and then forwards again) between edges, for example,
    // when teleporting and then being scattered backwards, or when restarting a path near the destination.
    private MapGraphEdgeIntraMap ProcessEdge_LineOfSightStartSkip(MapGraphEdgeIntraMap edge) {
        MapGridCell start = Player.Position.Grid(edge.Source.Map);

        int startIdx = 0;
        int endIdx = edge.Path.Count - 1;
        int lastIndexWithLOS = -1;

        while (startIdx <= endIdx) {
            int midIdx = startIdx + (endIdx - startIdx) / 2;
            MapGridCell end = edge.Path[midIdx].Grid(edge.Source.Map);
            MapGridLineOfSight los = edge.Source.Map.Grid.LineOfSight(start, end, costChangeIsOccluder: true);

            if (los.OccludedAt == null) {
                lastIndexWithLOS = midIdx;
                startIdx = midIdx + 1;
            } else {
                endIdx = midIdx - 1;
            }
        }

        if (lastIndexWithLOS > 0) {
            edge.Path.RemoveRange(0, lastIndexWithLOS);
            return edge with { Source = edge.Source with { Position = edge.Path[0] } };
        }

        return edge;
    }
}

public class ClickAheadMovementPlan(Vector2 start, Queue<Vector2> path, Map map) : ISocketEntityMovementPlan {
    public IReadOnlyCollection<Vector2> Path => _pathMovementPlan.Path;
    public bool Finished => _pathMovementPlan.Finished;
    public Vector2 Position  => _pathMovementPlan.Position;
    public Vector2 Goal => _clickAheadPoint;
    public Vector2 OriginalGoal => _pathMovementPlan.Goal;

    public bool Update(double dt, double speed) { 
        bool finished = _pathMovementPlan.Update(dt, speed);
        _clickAheadPoint = Position.SimpleDist(OriginalGoal) > MapGrid.CellWorldEpsilon ? CalculateClickAheadPoint(OriginalGoal, (float)speed) : OriginalGoal;
        return finished;
    }

    private readonly PathMovementPlan _pathMovementPlan = new(start, path);
    private Vector2 _clickAheadPoint = start;

    private Vector2 CalculateClickAheadPoint(Vector2 target, float speed) {
        Vector2 direction = Vector2.Normalize(target - Position);
        Vector2 clickAheadTarget = target + direction * speed * 0.33f;
        MapGridLineOfSight los = map.Grid.LineOfSight(Position.Grid(map), clickAheadTarget.Grid(map));
        return los.OccludedAt?.World(map) ?? clickAheadTarget;
    }
}
