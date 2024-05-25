using System.Diagnostics;
using System.Numerics;
using AdventureLandSharp.Core;
using AdventureLandSharp.Core.SocketApi;
using AdventureLandSharp.Core.Util;

namespace AdventureLandSharp;

public class MapGraphTraversal(Socket socket, IEnumerable<IMapGraphEdge> edges, MapLocation end) {
    public override string ToString() => $"End={End} Finished={Finished} CurrentEdge={CurrentEdge} PreviousEdge={PreviousEdge} NextEdge={NextEdge}";

    public MapLocation End => end;
    public bool Finished => _edges.Count == 0 && CurrentEdgeFinished;
    public IMapGraphEdge? CurrentEdge => _edge;
    public IMapGraphEdge? PreviousEdge => _lastEdge;
    public IMapGraphEdge? NextEdge => _edges.TryPeek(out IMapGraphEdge? nextEdge) ? nextEdge : null;

    public void Update() {
        DateTimeOffset now = DateTimeOffset.UtcNow;

        if (Finished) {
            return;
        }

        if (CurrentEdgeFinished) {
            Debug.Assert(_edges.Count > 0);
            _log.Debug($"Changing edge! Pos={Player.Position}, State={this}");
            _lastEdge = _edge;
            _edge = _edges.Dequeue();
            _edgeUpdate = now;
        }

        Debug.Assert(_edge != null);

        if (now >= _edgeUpdate) {
            ProcessEdge();
            _edgeUpdate = NextEdgeUpdate(now);
        }
    }

    private LocalPlayer Player => socket.Player;
    private readonly Queue<IMapGraphEdge> _edges = new(edges);
    private IMapGraphEdge? _edge;
    private IMapGraphEdge? _lastEdge;
    private DateTimeOffset _edgeUpdate;
    private readonly Logger _log = new(socket.Player.Name, "MapGraphTraversal");

    private bool CurrentEdgeFinished => _edge == null || _edge switch { 
        MapGraphEdgeInterMap interMap => Player.MapName == interMap.Dest.Map.Name && interMap.Type switch {
            MapConnectionType.Leave => Player.Position.Equivalent(interMap.Dest.Position, interMap.Dest.Map.DefaultSpawnScatter),
            _ => Player.Position == interMap.Dest.Position
        },
        MapGraphEdgeIntraMap intraMap => Player.Position == intraMap.Path[^1],
        MapGraphEdgeJoin join => Player.MapName == join.Dest.Map.Name && Player.Position == join.Dest.Position,
        MapGraphEdgeTeleport teleport => Player.Position.Equivalent(teleport.Dest.Position, teleport.Dest.Map.DefaultSpawnScatter),
        _ => true 
    };

    private DateTimeOffset NextEdgeUpdate(DateTimeOffset now) => _edge switch {
        MapGraphEdgeTeleport => now.Add(TimeSpan.FromSeconds(4.5)),
        _ => now.Add(TimeSpan.FromSeconds(0.25))
    };

    private void ProcessEdge() {
        _log.Debug($"{_edge}");

        if (_edge is not MapGraphEdgeIntraMap) {
            Player.MovementPlan = null;
        }

        if (_edge is MapGraphEdgeInterMap interMap) {
            if (interMap.Type is MapConnectionType.Door or MapConnectionType.Transporter) {
                socket.Emit<Outbound.Transport>(new(interMap.Dest.Map.Name, interMap.DestSpawnId));
            } else if (interMap.Type is MapConnectionType.Leave) {
                socket.Emit<Outbound.Leave>(new());
            } else {
                throw new NotImplementedException($"Unknown inter-map edge type: {interMap.Type}");
            }
        } else if (_edge is MapGraphEdgeJoin join) {
            socket.Emit<Outbound.Join>(new(join.JoinEventName));
        } else if (_edge is MapGraphEdgeIntraMap intraMap) {
            if (Player.MovementPlan?.Finished ?? true) {
                intraMap = ProcessEdge_MapTransitionDistanceSkip(intraMap);
                intraMap = ProcessEdge_LineOfSightMerge(intraMap);
                Player.MovementPlan = new ClickAheadMovementPlan(Player.Position, new(intraMap.Path), intraMap.Source.Map);
                _edge = intraMap;
            }
        } else if (_edge is MapGraphEdgeTeleport) {
            socket.Emit<Outbound.Town>(new());
        } else {
            throw new NotImplementedException($"Unknown edge type: {_edge}");
        }
    }

    // Given an intra-map edge, trim the path to the next door or transporter based on the next edge.
    // This allows us to skip walking right to the end of the edge to use e.g. a door which has a useable distance.
    private MapGraphEdgeIntraMap ProcessEdge_MapTransitionDistanceSkip(MapGraphEdgeIntraMap edge) {
        if (!_edges.TryPeek(out IMapGraphEdge? nextEdge) || nextEdge is not MapGraphEdgeInterMap nextEdgeInter) {
            return edge;
        }

        float cuttableDistance = UsableDistance(nextEdgeInter.Type) - MapGridTerrain.Epsilon*2;
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
            Debug.Assert(newLocation.SimpleDist(nextEdgeInter.Source.Position) < cuttableDistance);
            return edge with { DestPos = newLocation };
        }

        return edge;
    }

    // Given an intra-map edge, merges all of the steps that are within line of sight of each other,
    // so on and so forth until the edge is a series of connecting line of sight steps.
    private static MapGraphEdgeIntraMap ProcessEdge_LineOfSightMerge(MapGraphEdgeIntraMap edge) {
        int originalStartIdx = 0;

        while (originalStartIdx < edge.Path.Count) {
            MapGridCell start = edge.Path[originalStartIdx].Grid(edge.Map);
            int lastIndexWithLOS = -1;

            for (int startIdx = originalStartIdx + 1; startIdx < edge.Path.Count; ++startIdx) {
                MapGridCell pos = edge.Path[startIdx].Grid(edge.Map);
                MapGridLineOfSight los = MapGrid.LineOfSight(start, pos, c => c.Data(edge.Map).PHashScore > 2);

                if (los.Occluded.Count == 0) {
                    lastIndexWithLOS = startIdx;
                } else {
                    break;
                }
            }

            if (lastIndexWithLOS > originalStartIdx) {
                int cutIdxOneAfter = originalStartIdx + 1;
                int cutLength = lastIndexWithLOS - cutIdxOneAfter;

                if (cutLength > 0) {
                    edge.Path.RemoveRange(cutIdxOneAfter, cutLength);
                }
            }

            ++originalStartIdx;
        }

        return edge;
    }

    private static float UsableDistance(MapConnectionType Type) => Type switch {
        MapConnectionType.Door => GameConstants.DoorDist,
        MapConnectionType.Transporter => GameConstants.TransporterDist,
        _ => float.MaxValue
    };
}
