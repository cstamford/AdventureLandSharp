using AdventureLandSharp.Util;
using NetTopologySuite;
using NetTopologySuite.Geometries;
using NetTopologySuite.Index.Strtree;
using System.Collections.Concurrent;
using System.Numerics;

namespace AdventureLandSharp;

public static class Terrain {
    public const int CellSize = 8;
    public const int CellWallUnwalkable = CellSize;
    public const int CellWallAvoidance = 20;

    public static IReadOnlyDictionary<string, AStar.MapTerrainCell[,]> TerrainGrids => _terrain;

    public static Task GenerateTerrainGrids(GameData gameData) => Task.Run(() => {
        ConcurrentDictionary<string, AStar.MapTerrainCell[,]> grids = [];

        Parallel.ForEach(gameData.Geometry, levelData => {
            grids[levelData.Key] = CreateTerrain(levelData.Value);
        });

        _terrain = grids.ToDictionary();
    });

    public static AStar.GridPos WorldToGrid(double x, double y, GameLevelGeometry level) =>
        WorldToGrid(new((float)x, (float)y), level);

    public static AStar.GridPos WorldToGrid(Vector2 pos, GameLevelGeometry level) =>
        new((int)((pos.X - level.MinX) / CellSize), (int)((pos.Y - level.MinY) / CellSize));

    public static Vector2 GridToWorld(AStar.GridPos pos, GameLevelGeometry level) =>
        new((pos.X + 0.5f) * CellSize + level.MinX, (pos.Y + 0.5f) * CellSize + level.MinY);

    private static Dictionary<string, AStar.MapTerrainCell[,]> _terrain = [];

    private static AStar.MapTerrainCell[,] CreateTerrain(GameLevelGeometry level) {
        STRtree<LineString> spatial = new();

        foreach (int[] line in level.XLines ?? []) {
            LineString lineString = new([new(line[0], line[1]), new(line[0], line[2])]);
            spatial.Insert(lineString.EnvelopeInternal, lineString);
        }

        foreach (int[] line in level.YLines ?? []) {
            LineString lineString = new([new(line[1], line[0]), new(line[2], line[0])]);
            spatial.Insert(lineString.EnvelopeInternal, lineString);
        }

        spatial.Build();

        int width = (level.MaxX - level.MinX) / CellSize;
        int height = (level.MaxY - level.MinY) / CellSize;
        AStar.MapTerrainCell[,] grid = new AStar.MapTerrainCell[width, height];

        GeometryFactory fac = NtsGeometryServices.Instance.CreateGeometryFactory();

        for (int x = 0; x < width; ++x) {
            for (int y = 0; y < height; ++y) {
                int worldX = level.MinX + x * CellSize;
                int worldY = level.MinY + y * CellSize;

                Envelope cellEnvelope = new(worldX, worldX + CellSize, worldY, worldY + CellSize);
                Geometry cellGeometry = fac.ToGeometry(cellEnvelope);

                IList<LineString> query = spatial.Query(new Envelope(
                    worldX - CellSize - CellWallAvoidance,
                    worldX + CellSize + CellSize + CellWallAvoidance,
                    worldY - CellSize - CellWallAvoidance,
                    worldY + CellSize + CellSize + CellWallAvoidance
                ));

                bool walkable = true;
                float cost = 1.0f;

                if (query.Count > 0) {
                    walkable = !query.Any(l => l.Buffer(CellWallUnwalkable).Intersects(cellGeometry));
                    double dist = query.Min(l => l.Distance(new Point(worldX + CellSize / 2, worldY + CellSize / 2)));
                    const int range = CellWallAvoidance * 2;
                    cost += (float)((range - dist) / range);
                }

                grid[x, y] = new AStar.MapTerrainCell(walkable, cost);
            }
        }

        return grid;
    }
}
