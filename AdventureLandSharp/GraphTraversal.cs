namespace AdventureLandSharp;

public class GraphTraversal(Socket socket, IEnumerable<IMapGraphEdge> edges)
{
    private static readonly float _cellSizeEpsilon = MathF.Sqrt(
        MapGrid.CellSize * MapGrid.CellSize + MapGrid.CellSize * MapGrid.CellSize);

    private readonly Queue<IMapGraphEdge> _edges = new(edges);

    private DateTimeOffset _edgeUpdate;
    public IMapGraphEdge? Edge { get; private set; }

    public bool Finished => (Edge == null || CurrentEdgeFinished) && _edges.Count == 0;

    private LocalPlayer Player => socket.Player;

    private bool CurrentEdgeFinished => Edge != null && Edge switch
    {
        MapGraphEdgeInterMap interMap =>
            Player.MapName == interMap.Dest.Map.Name,
        MapGraphEdgeIntraMap intraMap =>
            Equivalent(Player.Position, intraMap.Dest.Location) ||
            Equivalent(Player.Position, Edge.Dest.Map.FindNearestWalkable(Edge.Dest.Location)),
        MapGraphEdgeTeleport teleport =>
            Equivalent(Player.Position, teleport.Dest.Location,
                MathF.Max(_cellSizeEpsilon, teleport.Dest.Map.DefaultSpawnScatter * 2)),
        _ => true
    };

    public void Update()
    {
        if (Finished) return;

        var now = DateTimeOffset.Now;

        if (CurrentEdgeFinished)
        {
            Edge = null;
            _edgeUpdate = now;
        }

        Edge ??= _edges.Dequeue();

        if (now >= _edgeUpdate)
        {
            ProcessEdge();
            _edgeUpdate = NextEdgeUpdate(now);
        }
    }

    private static bool Equivalent(Vector2 a, Vector2 b)
    {
        return Equivalent(a, b, _cellSizeEpsilon);
    }

    private static bool Equivalent(Vector2 a, Vector2 b, float epsilon)
    {
        return Vector2.Distance(a, b) <= epsilon;
    }

    private DateTimeOffset NextEdgeUpdate(DateTimeOffset now)
    {
        return Edge switch
        {
            MapGraphEdgeIntraMap => now.Add(TimeSpan.FromSeconds(0.1)),
            MapGraphEdgeInterMap => now.Add(TimeSpan.FromSeconds(3.0)),
            MapGraphEdgeTeleport => now.Add(TimeSpan.FromSeconds(4.5)),
            _ => DateTimeOffset.MaxValue
        };
    }

    private void ProcessEdge()
    {
        if (Edge is MapGraphEdgeInterMap interMap)
        {
            if (interMap.Type is MapConnectionType.Door or MapConnectionType.Transporter)
                EmitAndClearMovement<Outbound.Transport>(new Outbound.Transport(interMap.Dest.Map.Name,
                    interMap.DestSpawnId));
            else if (interMap.Type is MapConnectionType.Leave)
                EmitAndClearMovement<Outbound.Leave>(new Outbound.Leave());
            else
                throw new NotImplementedException($"Unknown inter-map edge type: {interMap.Type}");
        }
        else if (Edge is MapGraphEdgeIntraMap intraMap)
        {
            if (Player.MovementPlan == null)
            {
                var closestPointToUsIdx = intraMap.Path.FindIndex(p => Equivalent(p, Player.Position));
                if (closestPointToUsIdx != -1) intraMap.Path.RemoveRange(0, closestPointToUsIdx);
                Player.MovementPlan = new PathMovementPlan(Player.Position, new Queue<Vector2>(intraMap.Path));
            }
        }
        else if (Edge is MapGraphEdgeTeleport)
        {
            EmitAndClearMovement<Outbound.Town>(new Outbound.Town());
        }
        else
        {
            throw new NotImplementedException($"Unknown edge type: {Edge}");
        }
    }

    private void EmitAndClearMovement<T>(T data) where T : struct
    {
        socket.Emit(data);
        Player.MovementPlan = null;
    }
}