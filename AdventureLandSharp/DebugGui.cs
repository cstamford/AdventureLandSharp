using AdventureLandSharp.Core;
using AdventureLandSharp.Core.SocketApi;

namespace AdventureLandSharp;

#if WITH_GUI
using Raylib_cs;
using System.Numerics;

public class DebugGui {
    public DebugGui(World world, Socket socket) {
        Raylib.SetConfigFlags(ConfigFlags.ResizableWindow);
        Raylib.InitWindow(_width, _height, $"Adventure Land");
        _world = world;
        _socket = socket;
    }

    public void Update() {
        Raylib.BeginDrawing();
        Raylib.ClearBackground(Color.DarkGray);

        SocketEntityData player = _socket.Player;

        _cam.Offset = new(Raylib.GetRenderWidth() / 2.0f, Raylib.GetRenderHeight() / 2.0f);
        _cam.Target = player.Position;
        Raylib.BeginMode2D(_cam);

        Map map = _world.GetMap(player.Map);
        DrawMapGrid(map);
        DrawMapBoundaries(map);

        foreach (SocketEntityData entity in _socket.Entities) {
            Raylib.DrawCircle(
                (int)entity.Position.X,
                (int)entity.Position.Y,
                4,
                entity.Type == SocketEntityType.Player ? Color.Yellow : Color.Red);

            Raylib.DrawText(
                entity.Type == SocketEntityType.Player ? entity.Id : entity.TypeString,
                (int)entity.Position.X,
                (int)entity.Position.Y + 16,
                32,
                Color.White);
        }

        Raylib.DrawCircle((int)player.Position.X, (int)player.Position.Y, 4, Color.SkyBlue);

        if (_socket.PlayerMovementPlan is PathMovementPlan pathPlan) {
            DrawPath([.. pathPlan.Path], Color.Green);
        }

        Raylib.EndMode2D();
        Raylib.EndDrawing();
    }

    private const int _width = 1920;
    private const int _height = 1080;
    private Camera2D _cam = new(Vector2.Zero, Vector2.Zero, 0.0f, 1.0f);
    private readonly World _world;
    private readonly Socket _socket;

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
        GameLevelGeometry level = map.Geometry;

        int width = (level.MaxX - level.MinX) / MapGrid.CellSize;
        int height = (level.MaxY - level.MinY) / MapGrid.CellSize;

        float minCost = float.MaxValue;
        float maxCost = float.MinValue;

        for (int x = 0; x < width; x++) {
            for (int y = 0; y < height; y++) {
                MapGridCell gridCell = new(x, y);
                float cost = map.Grid.Cost(gridCell);

                if (cost < minCost) {
                    minCost = cost;
                }

                if (cost > maxCost) {
                    maxCost = cost;
                }
            }
        }

        float costRange = maxCost - minCost;

        for (int x = 0; x < width; x++) {
            for (int y = 0; y < height; y++) {
                MapGridCell gridCell = new(x, y);
                float normalizedCost = (map.Grid.Cost(gridCell) - minCost) / costRange;
                int worldX = level.MinX + x * MapGrid.CellSize;
                int worldY = level.MinY + y * MapGrid.CellSize;

                if (map.Grid.IsWalkable(gridCell)) {
                    Raylib.DrawRectangle(worldX, worldY, MapGrid.CellSize, MapGrid.CellSize, new(
                        (int)(255 * normalizedCost),
                        (int)(165 * normalizedCost),
                        0,
                        (int)(255 * normalizedCost)));
                } else {
                    Raylib.DrawRectangle(worldX, worldY, MapGrid.CellSize, MapGrid.CellSize, new(0, 0, 255, 255));
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
public class DebugGui {
    public DebugGui() { }
    public void Update(Socket socket) { }
}
#endif
