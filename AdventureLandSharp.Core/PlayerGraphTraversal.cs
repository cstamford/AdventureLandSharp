using System.Numerics;
using AdventureLandSharp.Core.SocketApi;

namespace AdventureLandSharp.Core;

public class PlayerGraphTraversal(Socket socket, Queue<IMapGraphEdge> edges) {
    public bool Finished => (_edge == null || CurrentEdgeFinished) && edges.Count == 0;

    public void Update() {
        if (Finished) {
            return;
        }

        DateTimeOffset now = DateTimeOffset.Now;

        if (CurrentEdgeFinished) {
            _edge = null;
            _edgeUpdate = now;
        }

        _edge ??= edges.Dequeue();

        if (now >= _edgeUpdate) {
            ProcessEdge();
            _edgeUpdate = NextEdgeUpdate(now);
        }
    }
    
    private SocketEntityData Player => socket.Player;

    private bool CurrentEdgeFinished => _edge != null && _edge switch { 
        MapGraphEdgeInterMap interMap => Player.Map == interMap.Dest.Map.Name,
        MapGraphEdgeIntraMap intraMap => Equivalent(Player.Position, intraMap.Dest.Location),
        MapGraphEdgeTeleport teleport => Equivalent(Player.Position, teleport.Dest.Location, 
            MathF.Max(_cellSizeEpsilon, teleport.Dest.Map.DefaultSpawnScatter*2)),
        _ => true 
    };

    private static readonly float _cellSizeEpsilon = MathF.Sqrt(
        MapGrid.CellSize*MapGrid.CellSize + MapGrid.CellSize*MapGrid.CellSize);
    private static bool Equivalent(Vector2 a, Vector2 b) => Equivalent(a, b, _cellSizeEpsilon);
    private static bool Equivalent(Vector2 a, Vector2 b, float epsilon) => Vector2.Distance(a, b) <= epsilon;

    private IMapGraphEdge? _edge;
    private DateTimeOffset _edgeUpdate;

    private DateTimeOffset NextEdgeUpdate(DateTimeOffset now) => _edge switch {
        MapGraphEdgeInterMap => now.Add(TimeSpan.FromSeconds(0.5)),
        MapGraphEdgeTeleport => now.Add(TimeSpan.FromSeconds(3.0)),
        _ => DateTimeOffset.MaxValue
    };

    private void ProcessEdge() {
        socket.PlayerMovementPlan = null; // Reset movement to avoid being accidentally jailed.

        if (_edge is MapGraphEdgeInterMap interMap) {
            if (interMap.Type is MapConnectionType.Door or MapConnectionType.Transporter) {
                socket.Emit<Outbound.Transport>(new(interMap.Dest.Map.Name, interMap.DestSpawnId));
            } else if (interMap.Type is MapConnectionType.Leave) {
                socket.Emit<Outbound.Leave>(new());
            } else {
                throw new NotImplementedException($"Unknown inter-map edge type: {interMap.Type}");
            }
        } else if (_edge is MapGraphEdgeIntraMap intraMap) {
            socket.PlayerMovementPlan = new PathMovementPlan(Player.Position, new(intraMap.Path));
        } else if (_edge is MapGraphEdgeTeleport) {
            socket.Emit<Outbound.Town>(new());
        } else {
            throw new NotImplementedException($"Unknown edge type: {_edge}");
        }
    }

}