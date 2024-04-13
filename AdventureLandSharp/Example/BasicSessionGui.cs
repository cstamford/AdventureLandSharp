using AdventureLandSharp.Core;
using AdventureLandSharp.Core.SocketApi;

namespace AdventureLandSharp.Example;

#if WITH_GUI
using Raylib_cs;
using System.Numerics;

public class BasicSessionGui : IDisposable {
    public BasicSessionGui(World world, Socket socket) {
        Raylib.SetConfigFlags(ConfigFlags.ResizableWindow);
        Raylib.InitWindow(_width, _height, $"Adventure Land: Basic GUI");
        _world = world;
        _socket = socket;
    }

    public bool Update() {
        Raylib.BeginDrawing();
        Raylib.ClearBackground(new Color(48, 48, 48, 255));

        LocalPlayer player = _socket.Player;

        _cam.Offset = new(Raylib.GetRenderWidth() / 2.0f, Raylib.GetRenderHeight() / 2.0f);
        _cam.Target = player.Position;
        Raylib.BeginMode2D(_cam);

        Map map = _world.GetMap(player.MapName);
        DrawMapGrid(map);
        DrawMapBoundaries(map);

        foreach (Entity e in _socket.Entities) {
            Color col = e switch { 
                Npc => Color.White,
                Monster => Color.Red,
                Player => Color.Purple,
                _ => throw new()
            };

            if (e.MovementPlan != null) {
                Raylib.DrawLine((int)e.Position.X, (int)e.Position.Y, (int)e.MovementPlan.Goal.X, (int)e.MovementPlan.Goal.Y, col);
            }

            Raylib.DrawCircle((int)e.Position.X, (int)e.Position.Y, 4, col);
            Raylib.DrawText(e.Name, (int)e.Position.X, (int)e.Position.Y - 16, 12, col);
        }

        Raylib.DrawCircle((int)player.Position.X, (int)player.Position.Y, 4, Color.SkyBlue);

        if (player.MovementPlan is PathMovementPlan pathPlan) {
            DrawPath([.. pathPlan.Path], Color.Green);
        }

        Raylib.EndMode2D();
        Raylib.EndDrawing();

        return !Raylib.WindowShouldClose();
    }

    public void Dispose() {
        if (_created) {
            Raylib.CloseWindow();
            _created = false;
        }
    }

    private const int _width = 1920;
    private const int _height = 1080;
    private Camera2D _cam = new(Vector2.Zero, Vector2.Zero, 0.0f, 1.0f);
    private readonly World _world;
    private readonly Socket _socket;
    private bool _created = true;

    private static void DrawMapBoundaries(Map map) {
        GameLevelGeometry level = map.Geometry;

        foreach (int[] line in level.XLines ?? []) {
            Raylib.DrawLine(line[0], line[1], line[0], line[2], Color.White);
        }

        foreach (int[] line in level.YLines ?? []) {
            Raylib.DrawLine(line[1], line[0], line[2], line[0], Color.White);
        }
    }

    private static void DrawMapGrid(Map map) {
        for (int x = 0; x < map.Grid.Width; x++) {
            for (int y = 0; y < map.Grid.Height; y++) {
                Vector2 pos = map.Grid.GridToWorld(new(x, y));
                if (map.Grid.IsWalkable(pos)) {
                    Raylib.DrawRectangle((int)pos.X, (int)pos.Y, MapGrid.CellSize, MapGrid.CellSize, new Color(64, 64, 64, 255));
                }
            }
        }
    }

    private static void DrawPath(IReadOnlyList<Vector2> path, Color color) {
        for (int i = 0; i < path.Count - 1; i++) {
            Raylib.DrawLineEx(path[i], path[i+1], 4.0f, color);
        }
    }
}
#else
public class BasicSessionGui : IDisposable {
    public BasicSessionGui() { }
    public bool Update(Socket socket) => false;
    public void Dispose() {}
}
#endif
