namespace AdventureLandSharp.Util;

public class AStarPath(AStar.MapTerrainCell[,] grid) {
    public AStar.GridPos Start { get; private set; }
    public AStar.GridPos End { get; private set; }
    public IReadOnlyList<AStar.GridPos> Path => _path ?? [];
    public IReadOnlyDictionary<AStar.GridPos, int> DebugVisitedCount => _debugVisitedCount ?? [];

    private List<AStar.GridPos>? _path;
    private Dictionary<AStar.GridPos, int>? _debugVisitedCount;

    private record PathJob(
        AStar.GridPos Start,
        AStar.GridPos End,
        List<AStar.GridPos> Path,
        int Steps,
        Dictionary<AStar.GridPos, int> DebugVisitedCount);

    private Task<PathJob>? _updateTask;

    public void Update(AStar.GridPos start, AStar.GridPos end, bool debug = false) {
        if (_updateTask?.IsCompleted ?? false) {
            _path = _updateTask.Result.Path;
            _debugVisitedCount = _updateTask.Result.DebugVisitedCount;
            _updateTask = null;
        }

        if (_updateTask == null && (_path == null || Start.Cost(start) >= 1.0f || End.Cost(end) >= 1.0f)) {
            Start = start;
            End = end;

            _updateTask = Task
                .Run(() => AStar.FindPath(grid, start, end, debug))
                .ContinueWith<PathJob>(t => new(
                    start,
                    end,
                    CleanPath(t.Result.Path),
                    t.Result.Steps,
                    t.Result.DebugVisitedCount));
        }
    }

    public void Invalidate() {
        _updateTask?.Wait();
        _path?.Clear();
        _debugVisitedCount?.Clear();
    }

    private static List<AStar.GridPos> CleanPath(List<AStar.GridPos> path) {
        List<AStar.GridPos> mergedPath = [];

        if (path.Count == 0) {
            return mergedPath;
        }

        AStar.GridPos prevDir = default;

        for (int i = 1; i < path.Count; i++) {
            AStar.GridPos dir = new(path[i].X - path[i - 1].X, path[i].Y - path[i - 1].Y);

            if (dir != prevDir) {
                mergedPath.Add(path[i - 1]);
            }

            prevDir = dir;
        }

        mergedPath.Add(path.Last());
        return mergedPath;
    }
}
