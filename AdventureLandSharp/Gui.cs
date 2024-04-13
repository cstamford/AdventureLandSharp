using Raylib_cs;

namespace AdventureLandSharp;

#if WITH_GUI


public class GameGui : IDisposable
{
    private const int _width = 1920;
    private const int _height = 1080;
    private readonly Socket _socket;
    private readonly World _world;
    private Camera2D _cam = new(Vector2.Zero, Vector2.Zero, 0.0f, 1.0f);

    public GameGui(World world, Socket socket)
    {
        Raylib.SetConfigFlags(ConfigFlags.ResizableWindow);
        Raylib.InitWindow(_width, _height, "Adventure Land");
        _world = world;
        _socket = socket;
    }

    public void Dispose()
    {
        Raylib.CloseWindow();
    }

    public bool Update()
    {
        Raylib.BeginDrawing();
        Raylib.ClearBackground(new Color(48, 48, 48, 255));

        var player = _socket.Player;

        _cam.Offset = new Vector2(Raylib.GetRenderWidth() / 2.0f, Raylib.GetRenderHeight() / 2.0f);
        _cam.Target = player.Position;
        Raylib.BeginMode2D(_cam);

        var map = _world.GetMap(player.MapName);
        DrawMapGrid(map);
        DrawMapBoundaries(map);

        foreach (var e in _socket.Entities)
        {
            var col = e switch
            {
                Npc => Color.White,
                Monster => Color.Red,
                Player => Color.Purple,
                _ => throw new Exception()
            };

            if (e.MovementPlan != null)
                Raylib.DrawLine((int) e.Position.X, (int) e.Position.Y, (int) e.MovementPlan.Goal.X,
                    (int) e.MovementPlan.Goal.Y, col);

            Raylib.DrawCircle((int) e.Position.X, (int) e.Position.Y, 4, col);
            Raylib.DrawText(e.Name, (int) e.Position.X, (int) e.Position.Y - 16, 12, col);
        }

        Raylib.DrawCircle((int) player.Position.X, (int) player.Position.Y, 4, Color.SkyBlue);

        if (player.MovementPlan is PathMovementPlan pathPlan) DrawPath([.. pathPlan.Path], Color.Green);

        Raylib.EndMode2D();
        Raylib.EndDrawing();

        return !Raylib.WindowShouldClose();
    }

    private static void DrawMapBoundaries(Map map)
    {
        var level = map.Geometry;

        foreach (var line in level.XLines ?? []) Raylib.DrawLine(line[0], line[1], line[0], line[2], Color.White);

        foreach (var line in level.YLines ?? []) Raylib.DrawLine(line[1], line[0], line[2], line[0], Color.White);
    }

    private static void DrawMapGrid(Map map)
    {
        for (var x = 0; x < map.Grid.Width; x++)
        for (var y = 0; y < map.Grid.Height; y++)
        {
            var pos = map.Grid.GridToWorld(new MapGridCell(x, y));
            if (map.Grid.IsWalkable(pos))
                Raylib.DrawRectangle((int) pos.X, (int) pos.Y, MapGrid.CellSize, MapGrid.CellSize,
                    new Color(64, 64, 64, 255));
        }
    }

    private static void DrawPath(IReadOnlyList<Vector2> path, Color color)
    {
        for (var i = 0; i < path.Count - 1; i++) Raylib.DrawLineEx(path[i], path[i + 1], 4.0f, color);
    }
}
#else
public class GameGui : IDisposable {
    public GameGui() { }
    public bool Update(Socket socket) => false;
    public void Dispose() {}
}
#endif