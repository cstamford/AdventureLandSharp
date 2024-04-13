using System.Numerics;
using AdventureLandSharp.Core;
using AdventureLandSharp.Core.SocketApi;

namespace AdventureLandSharp;

public class MapGraphTraversal(Socket socket, IEnumerable<IMapGraphEdge> edges) {
    public IMapGraphEdge? Edge => _edge;
    public bool Finished => (_edge == null || CurrentEdgeFinished) && _edges.Count == 0;

    public void Update() {
        if (Finished) {
            return;
        }

        DateTimeOffset now = DateTimeOffset.Now;

        if (CurrentEdgeFinished) {
            _edge = null;
            _edgeUpdate = now;
        }

        _edge ??= _edges.Dequeue();

        if (now >= _edgeUpdate) {
            ProcessEdge();
            _edgeUpdate = NextEdgeUpdate(now);
        }
    }
    
    private LocalPlayer Player => socket.Player;
    private readonly Queue<IMapGraphEdge> _edges = new(edges);

    private bool CurrentEdgeFinished => _edge != null && _edge switch { 
        MapGraphEdgeInterMap interMap => 
            Player.MapName == interMap.Dest.Map.Name,
        MapGraphEdgeIntraMap intraMap => 
            Equivalent(Player.Position, intraMap.Dest.Location) || 
            Equivalent(Player.Position, _edge.Dest.Map.FindNearestWalkable(_edge.Dest.Location)),
        MapGraphEdgeTeleport teleport => 
            Equivalent(Player.Position, teleport.Dest.Location, MathF.Max(_cellSizeEpsilon, teleport.Dest.Map.DefaultSpawnScatter*2)),
        _ => true 
    };

    private static readonly float _cellSizeEpsilon = MathF.Sqrt(
        MapGrid.CellSize*MapGrid.CellSize + MapGrid.CellSize*MapGrid.CellSize);
    private static bool Equivalent(Vector2 a, Vector2 b) => Equivalent(a, b, _cellSizeEpsilon);
    private static bool Equivalent(Vector2 a, Vector2 b, float epsilon) => Vector2.Distance(a, b) <= epsilon;

    private IMapGraphEdge? _edge;
    private DateTimeOffset _edgeUpdate;

    private DateTimeOffset NextEdgeUpdate(DateTimeOffset now) => _edge switch {
        MapGraphEdgeIntraMap => now.Add(TimeSpan.FromSeconds(0.1)),
        MapGraphEdgeInterMap => now.Add(TimeSpan.FromSeconds(3.0)),
        MapGraphEdgeTeleport => now.Add(TimeSpan.FromSeconds(4.5)),
        _ => DateTimeOffset.MaxValue
    };

    private void ProcessEdge() {
        if (_edge is MapGraphEdgeInterMap interMap) {
            if (interMap.Type is MapConnectionType.Door or MapConnectionType.Transporter) {
                EmitAndClearMovement<Outbound.Transport>(new(interMap.Dest.Map.Name, interMap.DestSpawnId));
            } else if (interMap.Type is MapConnectionType.Leave) {
                EmitAndClearMovement<Outbound.Leave>(new());
            } else {
                throw new NotImplementedException($"Unknown inter-map edge type: {interMap.Type}");
            }
        } else if (_edge is MapGraphEdgeIntraMap intraMap) {
            if (Player.MovementPlan == null) {
                _edge = ProcessEdge_TrimIntraMap(intraMap); // note: updates intraMap.Path in-place
                Player.MovementPlan = new ClickAheadMovementPlan(Player.Position, new(intraMap.Path), intraMap.Source.Map);
            }
        } else if (_edge is MapGraphEdgeTeleport) {
            EmitAndClearMovement<Outbound.Town>(new());
        } else {
            throw new NotImplementedException($"Unknown edge type: {_edge}");
        }
    }

    private void EmitAndClearMovement<T>(T data) where T: struct {
        socket.Emit(data);
        Player.MovementPlan = null;
    }

    private MapGraphEdgeIntraMap ProcessEdge_TrimIntraMap(MapGraphEdgeIntraMap edge) {
        if (_edges.TryPeek(out IMapGraphEdge? nextEdge) && nextEdge is MapGraphEdgeInterMap nextEdgeInter) {
            const float cutPadding = MapGrid.CellSize * -4;

            float cuttableDistance = cutPadding + nextEdgeInter.Type switch {
                MapConnectionType.Door => GameConstants.DoorDist,
                MapConnectionType.Transporter => GameConstants.TransporterDist,
                _ => 0.0f
            };

            if (cuttableDistance > 0) {
                int cutIdx = edge.Path.FindIndex(x => Vector2.Distance(x, edge.Dest.Location) < cuttableDistance);
                edge.Path.RemoveRange(cutIdx, edge.Path.Count - cutIdx);
                Vector2 newLocation = edge.Path.Count > 0 ? edge.Path[^1] : Player.Position;
                return edge with { Dest = edge.Dest with { Location = newLocation } };
            }
        }

        return edge;
    }
}

public class ClickAheadMovementPlan(Vector2 start, Queue<Vector2> path, Map map) : ISocketEntityMovementPlan {
    public Queue<Vector2> Path => _pathMovementPlan.Path;
    public bool Finished => _pathMovementPlan.Finished;
    public Vector2 Position  => _pathMovementPlan.Position;
    public Vector2 Goal => _clickAheadPoint;

    public bool Update(double dt, double speed) { 
        _clickAheadPoint = Path.Count > 1 ? CalculateClickAheadPoint(_pathMovementPlan.Goal, (float)speed) : _pathMovementPlan.Goal;
        return _pathMovementPlan.Update(dt, speed);
    }

    private readonly PathMovementPlan _pathMovementPlan = new(start, path);
    private Vector2 _clickAheadPoint = start;

    private Vector2 CalculateClickAheadPoint(Vector2 target, float speed) {
        Vector2 direction = Vector2.Normalize(target - Position);
        Vector2 clickAheadTarget = target + direction * speed * 0.33f;
        MapGridLineOfSight los = map.Grid.LineOfSight(map.Grid.WorldToGrid(Position), map.Grid.WorldToGrid(clickAheadTarget));
        return los.OccludedAt != null ? map.Grid.GridToWorld(los.OccludedAt.Value) : clickAheadTarget;
    }
}