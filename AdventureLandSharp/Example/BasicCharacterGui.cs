using AdventureLandSharp.Core;
using AdventureLandSharp.Core.SocketApi;
using AdventureLandSharp.Interfaces;

namespace AdventureLandSharp.Example;

#if WITH_GUI
using Raylib_cs;
using System.Diagnostics;
using System.Numerics;

public class BasicCharacterGui : ISessionGui {
    public BasicCharacterGui(World world, ICharacter character) {
        Raylib.SetConfigFlags(ConfigFlags.ResizableWindow | ConfigFlags.AlwaysRunWindow);
        Raylib.InitWindow(_width, _height, $"Adventure Land: Basic GUI");
        _world = world;
        _character = character;
    }

    public bool Update() {
        Debug.Assert(Raylib.IsWindowReady());

        Draw_1_Pre();
        Draw_2_Background();
        Draw_3_Mid();
        Draw_4_Foreground();
        Draw_5_Post();

        return !Raylib.WindowShouldClose();
    }

    protected virtual void Draw_1_Pre() {
        Raylib.BeginDrawing();
        Raylib.ClearBackground(new Color(48, 48, 48, 255));

        _cam.Offset = new(Raylib.GetRenderWidth() / 2.0f, Raylib.GetRenderHeight() / 2.0f);
        _cam.Target = Character.Entity.Position;
        _camBounds = new(
            _cam.Target.X - _cam.Offset.X,
            _cam.Target.Y - _cam.Offset.Y,
            _cam.Offset.X*2,
            _cam.Offset.Y*2);

        Raylib.BeginMode2D(_cam);
    }

    protected virtual void Draw_2_Background() {
        Map map = _world.GetMap(Character.Entity.MapName);
        DrawMapGridBackground(map, _camBounds);

        if (map.Smap != null) {
            if (_smapDraw) {
                DrawSmap(map, _camBounds);
            }

            if (_smapCompareRpToP) {
                DrawSmapRpVsP(map, _camBounds);
            }
        }

        DrawMapBoundaries(map);
        DrawMapConnections(map);
    }

    protected virtual void Draw_3_Mid() {
        foreach (DropData drop in Character.Socket.Drops) {
            Raylib.DrawRectangle((int)drop.Position.X, (int)drop.Position.Y, 4, 4, Color.Brown);
            Raylib.DrawText(drop.Id, (int)drop.Position.X, (int)drop.Position.Y - 16, 12, Color.Brown);
        }
    }

    protected virtual void Draw_4_Foreground() {
        foreach (Entity e in Character.Socket.Entities) {
            Color col = e switch { 
                Npc => Color.White,
                Monster => Color.Red,
                Player => Color.Purple,
                _ => throw new()
            };

            DrawMovementPlanForEntity(e, col);
            Raylib.DrawCircle((int)e.Position.X, (int)e.Position.Y, 4, col);
            Raylib.DrawText($"{e.Name}/{e.Type}/{e.Id}", (int)e.Position.X, (int)e.Position.Y - 16, 12, col);
        }

        Raylib.DrawCircle((int)Character.Entity.Position.X, (int)Character.Entity.Position.Y, 4, Color.SkyBlue);
        DrawMovementPlanForEntity(Character.Entity, Color.Green);
    }

    protected virtual void Draw_5_Post() {
        Raylib.EndMode2D();
        Raylib.DrawFPS(8, 8);
        Raylib.EndDrawing();
    }

    protected ICharacter Character => _character;
    protected World World => _world;
    protected Rectangle CamBounds => _camBounds;

    private const int _width = 1920;
    private const int _height = 1080;
    private const bool _smapDraw = false;
    private const bool _smapCompareRpToP = false;

    private readonly World _world;
    private readonly ICharacter _character;
    private Camera2D _cam = new(Vector2.Zero, Vector2.Zero, 0.0f, 1.0f);
    private Rectangle _camBounds = new(0, 0, _width, _height);

    private static void DrawMapBoundaries(Map map) {
        GameLevelGeometry level = map.Geometry;

        foreach (int[] line in level.XLines ?? []) {
            Raylib.DrawLine(line[0], line[1], line[0], line[2], Color.White);
        }

        foreach (int[] line in level.YLines ?? []) {
            Raylib.DrawLine(line[1], line[0], line[2], line[0], Color.White);
        }
    }

    private static void DrawMapGridBackground(Map map, Rectangle bounds) {
        MapGridTerrain terrain = map.Grid.Terrain;
        float minCost = terrain.ToEnumerable().Min(x => x.Cost);
        float maxCost = terrain.ToEnumerable().Max(x => x.Cost);

        for (int x = 0; x < terrain.Width; x++) {
            for (int y = 0; y < terrain.Height; y++) {
                MapGridCell cell = new(x, y);
                MapGridCellData cellData = terrain[cell];
                Vector2 pos = cell.World(map);

                if (pos.X < bounds.X || pos.X > bounds.X + bounds.Width || pos.Y < bounds.Y || pos.Y > bounds.Y + bounds.Height) {
                    continue;
                }

                if (cellData.IsWalkable) {
                    Raylib.DrawRectangle((int)pos.X, (int)pos.Y, MapGridTerrain.CellSize, MapGridTerrain.CellSize, new Color(64, 64, 64, 255));

                    if (cellData.Cost > 1) {
                        float costZeroToOne = (cellData.Cost - minCost) / (maxCost - minCost);
                        Raylib.DrawRectangle((int)pos.X, (int)pos.Y, MapGridTerrain.CellSize, MapGridTerrain.CellSize, new Color((int)(255 * costZeroToOne), 0, 0, 255));
                    }
                }
            }
        }
    }

    private static void DrawSmap(Map map, Rectangle bounds) {
        foreach (KeyValuePair<GameDataSmapCell, GameDataSmapCellData> cell in map.Smap!.Data) {
            Vector2 pos = new(cell.Key.X, cell.Key.Y);

            if (pos.X < bounds.X || pos.X > bounds.X + bounds.Width || pos.Y < bounds.Y || pos.Y > bounds.Y + bounds.Height) {
                continue;
            }

            if (cell.Value.Value > 0) {
                Raylib.DrawRectangle((int)pos.X, (int)pos.Y, GameDataSmap.StepSize, GameDataSmap.StepSize, ColorFromValue(cell.Value));

                if (!cell.Value.IsValid) {
                    Raylib.DrawLine((int)pos.X, (int)pos.Y, (int)pos.X + GameDataSmap.StepSize, (int)pos.Y + GameDataSmap.StepSize, Color.Black);
                    Raylib.DrawLine((int)pos.X + GameDataSmap.StepSize, (int)pos.Y, (int)pos.X, (int)pos.Y + GameDataSmap.StepSize, Color.Black);
                }
            }
        }

        static Color ColorFromValue(GameDataSmapCellData cellData) {
            float t = ((float)cellData.Value - GameDataSmapCellData.MinValue) / (GameDataSmapCellData.MaxValue - GameDataSmapCellData.MinValue);
            byte r = 255;
            byte g = (byte)(255 * (1 - t));
            byte b = 0;
            byte a = 255;
            return new(r, g, b, a);
        }
    }

    private static void DrawSmapRpVsP(Map map, Rectangle bounds) {
        MapGridTerrain terrain = map.Grid.Terrain;

        for (int x = 0; x < terrain.Width; x++) {
            for (int y = 0; y < terrain.Height; y++) {
                MapGridCell cell = new(x, y);
                Vector2 pos = cell.World(map);

                if (pos.X < bounds.X || pos.X > bounds.X + bounds.Width || pos.Y < bounds.Y || pos.Y > bounds.Y + bounds.Height) {
                    continue;
                }

                GameDataSmapCellData cellDataRp = pos.RpHash(map);
                GameDataSmapCellData cellDataP = pos.PHash(map);

                if (cellDataRp.Value != cellDataP.Value) {
                    Raylib.DrawRectangle((int)pos.X, (int)pos.Y, MapGridTerrain.CellSize, MapGridTerrain.CellSize, Color.Pink);
                }
            }
        }
    }

    private static void DrawMapConnections(Map map) {
        foreach (MapConnection conn in map.Connections.Where(x => x.SourceMap == map.Name)) {
            Raylib.DrawText(conn.DestMap, (int)conn.SourceX, (int)conn.SourceY - 16, 12, Color.Gold);
            Raylib.DrawRectangle((int)conn.SourceX, (int)conn.SourceY, 8, 8, Color.Gold);
        }
    }

    private static void DrawPath(IReadOnlyList<Vector2> path, Color color) {
        for (int i = 0; i < path.Count - 1; i++) {
            Raylib.DrawLineEx(path[i], path[i+1], 1, color);
            Raylib.DrawCircle((int)path[i].X, (int)path[i].Y, 2, color);
        }

        Vector2 last = path[^1];
        Raylib.DrawCircle((int)last.X, (int)last.Y, 2, color);
    }

    private static void DrawMovementPlanForEntity(Entity e, Color color) {
        ISocketEntityMovementPlan? plan = e.MovementPlan;
        while (plan is ISocketEntityMovementPlanModulator modulator) {
            plan = modulator.Plan;
        }

        IReadOnlyCollection<Vector2> path = plan switch {
            DestinationMovementPlan planDest => new Queue<Vector2>([planDest.Goal]),
            PathMovementPlan planPath => planPath.Path,
            ClickAheadMovementPlan planCa => planCa.Path,
            _ => []
        };

        if (path.Count > 0) {
            Raylib.DrawLineEx(e.Position, path.First(), 1, color);
            DrawPath([.. path], color);
        }

        if (plan is ClickAheadMovementPlan ca) {
            Raylib.DrawLineEx(ca.OriginalGoal, ca.Goal, 1, Color.Lime);
            Raylib.DrawCircle((int)ca.Goal.X, (int)ca.Goal.Y, 2, Color.Lime);
        }
    }

    public void Dispose() {
        DisposeInternal();
        GC.SuppressFinalize(this);
    }

    protected virtual void DisposeInternal() {
        Raylib.CloseWindow();
    }
}
#else
public class BasicCharacterGui(World world, ICharacter character) : ISessionGui {
    public bool Update() => true;
    public void Dispose() { }
    protected virtual void Draw_1_Pre() { }
    protected virtual void Draw_2_Background() { }
    protected virtual void Draw_3_Mid() { }
    protected virtual void Draw_4_Foreground() { }
    protected virtual void Draw_5_Post() { }
    protected virtual ICharacter Character { get; set; } = default!;
    protected World World => default!;
}
#endif
