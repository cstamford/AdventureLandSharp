using AdventureLandSharp.Socket;
using AdventureLandSharp.Util;
using Raylib_cs;
using System.Numerics;

namespace AdventureLandSharp;

public class AdventureLandRenderer : IDisposable {
    public AdventureLandRenderer(AdventureLand al) {
        _al = al;
        Raylib.InitWindow(_width, _height, "AdventureLand");
        Raylib.SetTargetFPS(_fps);
    }

    public void Update(SocketData socket) {
        SocketEntity player = socket.GetPlayer();
        List<SocketEntity> entities = socket.GetEntities();

        Raylib.BeginDrawing();
        Raylib.ClearBackground(Color.DarkGray);

        _cam.Target = new((float)player.X, (float)player.Y);
        Raylib.BeginMode2D(_cam);

        DrawEntity(player);

        foreach (SocketEntity entity in entities) {
            DrawEntity(entity);
        }

        string mapName = socket.GetMap();

        if (Terrain.TerrainGrids.TryGetValue(mapName, out AStar.MapTerrainCell[,]? terrainGrid) &&
            _al.Game.Geometry.TryGetValue(mapName, out GameLevelGeometry geometry)) {
            if (Raylib.IsMouseButtonPressed(MouseButton.Left)) {
                Vector2 worldPos = Raylib.GetScreenToWorld2D(Raylib.GetMousePosition(), _cam);
                _al.SetGoalPosition(worldPos, terrainGrid);
            }

            DrawGrid(geometry, terrainGrid);
            DrawLevel(geometry);
        }

        Raylib.EndMode2D();
        Raylib.EndDrawing();
    }

    public void Dispose() {
        Raylib.CloseWindow();
    }

    private readonly AdventureLand _al;

    private const int _width = 1920;
    private const int _height = 1080;
    private const int _fps = 120;

    private Camera2D _cam = new(new Vector2((int)(_width / 2.0f), (int)(_height / 2.0f)), new Vector2(0, 0), 0.0f, 0.5f);

    private void DrawEntity(SocketEntity entity) {
        Raylib.DrawCircle((int)entity.X, (int)entity.Y, 8, Color.Red);
        Raylib.DrawText(entity.Id, (int)entity.X, (int)entity.Y + 16, 16, Color.White);
    }

    private void DrawLevel(GameLevelGeometry level) {
        foreach (int[] line in level.XLines ?? []) {
            Raylib.DrawLine(line[0], line[1], line[0], line[2], Color.White);
        }

        foreach (int[] line in level.YLines ?? []) {
            Raylib.DrawLine(line[1], line[0], line[2], line[0], Color.White);
        }
    }

    private void DrawGrid(GameLevelGeometry level, AStar.MapTerrainCell[,] terrainGrid) {
        int width = (level.MaxX - level.MinX) / Terrain.CellSize;
        int height = (level.MaxY - level.MinY) / Terrain.CellSize;

        for (int x = 0; x < width; x++) {
            for (int y = 0; y < height; y++) {
                int worldX = level.MinX + x * Terrain.CellSize;
                int worldY = level.MinY + y * Terrain.CellSize;

                if (!terrainGrid[x, y].Walkable) {
                    Raylib.DrawRectangle(worldX, worldY, Terrain.CellSize, Terrain.CellSize, Color.Red);
                } else if (terrainGrid[x, y].Cost >= 1.5) {
                    Raylib.DrawRectangle(worldX, worldY, Terrain.CellSize, Terrain.CellSize, Color.Orange);
                } else if (terrainGrid[x, y].Cost > 1) {
                    Raylib.DrawRectangle(worldX, worldY, Terrain.CellSize, Terrain.CellSize, Color.Yellow);
                }
            }
        }

        if (_al.PlayerPath?.Path.Count > 0) {
            IReadOnlyList<AStar.GridPos> path = _al.PlayerPath.Path;
            for (int i = 0; i < path.Count - 1; i++) {
                Vector2 start = Terrain.GridToWorld(path[i], level);
                Vector2 end = Terrain.GridToWorld(path[i + 1], level);
                Raylib.DrawLineEx(start, end, 4.0f, Color.SkyBlue);
            }
        }
    }
}
