using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using AdventureLandSharp.Core.Util;
using NetTopologySuite.Geometries;
using NetTopologySuite.Index.Strtree;

namespace AdventureLandSharp.Core;

public class MapGridTerrain(GameDataMap map, GameLevelGeometry geo, GameDataSmap? smap) {
    public const int CellSize = 5;
    public static readonly float Epsilon = MathF.Sqrt(CellSize*CellSize + CellSize*CellSize);
    public const int CellWallUnwalkable = CellSize/2;
    public const int CellWallAvoidance = CellWallUnwalkable + GameConstants.PlayerWidth/2;
    public const int CellWallSpatialQuery = CellWallAvoidance + 16;

    public MapGridCellData this[MapGridCell cell] => cell.X < 0 || cell.X >= _width || cell.Y < 0 || cell.Y >= _height ? MapGridCellData.Unwalkable : _terrain[cell.X, cell.Y];
    public IEnumerable<MapGridCellData> ToEnumerable() => _terrain.ToEnumerable();

    public int Width => _width;
    public int Height => _height;
    public MapGridCellData[,] Terrain => _terrain;

    public MapGridCell Grid(Vector2 pos) => Grid(geo, pos);
    public Vector2 World(MapGridCell pos) => World(geo, pos);

    private readonly MapGridCellData[,] _terrain = smap == null ? 
        CreateTerrainFromGeometry(map, geo) : 
        PopulateTerrainFromSMap(geo, smap);
    private readonly int _width = (geo.MaxX - geo.MinX) / CellSize;
    private readonly int _height = (geo.MaxY - geo.MinY) / CellSize;

    private static MapGridCell Grid(GameLevelGeometry geo, Vector2 pos) => new(
        (int)((pos.X - geo.MinX) / CellSize), 
        (int)((pos.Y - geo.MinY) / CellSize)
    );

    private static Vector2 World(GameLevelGeometry geo, MapGridCell pos) => new(
        geo.MinX + pos.X * CellSize, 
        geo.MinY + pos.Y * CellSize
    );

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private static MapGridCellData[,] PopulateTerrainFromSMap(GameLevelGeometry geo, GameDataSmap? smap) {
        Debug.Assert(smap != null);

        int width = (geo.MaxX - geo.MinX) / CellSize;
        int height = (geo.MaxY - geo.MinY) / CellSize;

        MapGridCellData[,] grid = new MapGridCellData[width, height];

        Parallel.For(0, width, x => {
            for (int y = 0; y < height; ++y) {
                Vector2 topLeft = new(geo.MinX + x * CellSize, geo.MinY + y * CellSize);
                Vector2 topRight = topLeft + new Vector2(CellSize, 0);
                Vector2 bottomLeft = topLeft + new Vector2(0, CellSize);
                Vector2 bottomRight = topLeft + new Vector2(CellSize, CellSize);

                Span<GameDataSmapCellData> rpHashData = [smap.RpHash(topLeft), smap.RpHash(topRight), smap.RpHash(bottomLeft), smap.RpHash(bottomRight)];
                Span<GameDataSmapCellData> pHashData = [smap.PHash(topLeft), smap.PHash(topRight), smap.PHash(bottomLeft), smap.PHash(bottomRight)];

                rpHashData.Sort((x, y) => y.Value.CompareTo(x.Value));
                pHashData.Sort((x, y) => y.Value.CompareTo(x.Value));

                grid[x, y] = MapGridCellData.Walkable with {
                    RpHashScore = rpHashData[0].Value,
                    PHashScore = pHashData[0].Value
                };
            }
        });

        return grid;
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private static MapGridCellData[,] CreateTerrainFromGeometry(GameDataMap map, GameLevelGeometry geo) {
        STRtree<LineString> spatial = new();

        foreach (int[] line in geo.XLines ?? []) {
            LineString lineString = new([new(line[0], line[1]), new(line[0], line[2])]);
            spatial.Insert(lineString.EnvelopeInternal, lineString);
        }

        foreach (int[] line in geo.YLines ?? []) {
            LineString lineString = new([new(line[1], line[0]), new(line[2], line[0])]);
            spatial.Insert(lineString.EnvelopeInternal, lineString);
        }

        spatial.Build();

        int width = (geo.MaxX - geo.MinX) / CellSize;
        int height = (geo.MaxY - geo.MinY) / CellSize;
        MapGridCellData[,] grid = new MapGridCellData[width, height];

        GeometryFactory fac = GeometryFactory.FloatingSingle;

        Parallel.For(0, width, x => {
            for (int y = 0; y < height; ++y) {
                int worldX = geo.MinX + x * CellSize;
                int worldY = geo.MinY + y * CellSize;

                // Create a spatial query covering the total area we care about overlaps in.
                IList<LineString> broadQuery = spatial.Query(new Envelope(
                    worldX - CellWallSpatialQuery,
                    worldX + CellSize + CellWallSpatialQuery,
                    worldY - CellWallSpatialQuery,
                    worldY + CellSize + CellWallSpatialQuery
                ));

                if (broadQuery.Count == 0) { // There's no overlap at all. We can just move on.
                    grid[x, y] = MapGridCellData.Walkable;
                    continue;
                }

                // Create geometry for the cell itself - we check this against anything overlapping the spatial query.
                // - Anything too close to the wall is marked as unwalkable.
                // - Anything within the wall avoidance distance is marked as walkable, but with a scaling penalty.

                Envelope cellEnvelope = new(worldX, worldX + CellSize, worldY, worldY + CellSize);
                Geometry cellGeometry = fac.ToGeometry(cellEnvelope);
                double dist = broadQuery.Min(l => l.Distance(cellGeometry));
                double distCost = (dist > CellWallAvoidance) ? 0 : (CellWallAvoidance - Math.Min(CellWallUnwalkable + dist, CellWallAvoidance)) / CellWallAvoidance;
                MapGridCellData cell = dist > CellWallUnwalkable ? MapGridCellData.Walkable with { Cost = (float)(1 + distCost) } : MapGridCellData.Unwalkable;

                // Calculate a score which represents "how much of a corner is this?".

                Coordinate center = new(worldX + CellSize/2, worldY + CellSize/2);
                LineString lineNW = fac.CreateLineString([center, new(worldX - CellWallSpatialQuery, worldY - CellWallSpatialQuery)]);
                LineString lineN = fac.CreateLineString([center, new(worldX, worldY - CellWallSpatialQuery)]); 
                LineString lineNE = fac.CreateLineString([center, new(worldX + CellWallSpatialQuery, worldY - CellWallSpatialQuery)]);
                LineString lineE = fac.CreateLineString([center, new(worldX + CellWallSpatialQuery, worldY)]);
                LineString lineSE = fac.CreateLineString([center, new(worldX + CellWallSpatialQuery, worldY + CellWallSpatialQuery)]);
                LineString lineS = fac.CreateLineString([center, new(worldX, worldY + CellWallSpatialQuery)]);
                LineString lineSW = fac.CreateLineString([center, new(worldX - CellWallSpatialQuery, worldY + CellWallSpatialQuery)]);
                LineString lineW = fac.CreateLineString([center, new(worldX - CellWallSpatialQuery, worldY)]);

                double NW = broadQuery.Min(l => l.Distance(lineNW));
                double N = broadQuery.Min(l => l.Distance(lineN));
                double NE = broadQuery.Min(l => l.Distance(lineNE));
                double E = broadQuery.Min(l => l.Distance(lineE));
                double SE = broadQuery.Min(l => l.Distance(lineSE));
                double S = broadQuery.Min(l => l.Distance(lineS));
                double SW = broadQuery.Min(l => l.Distance(lineSW));
                double W = broadQuery.Min(l => l.Distance(lineW));

                double maxDist = CellWallSpatialQuery*2 + CellSize/2;

                grid[x, y] = cell with { 
                    CornerScore = (float)((
                        (1 - NW / maxDist) + 
                        (1 - N / maxDist) + 
                        (1 - NE / maxDist) + 
                        (1 - E / maxDist) + 
                        (1 - SE / maxDist) + 
                        (1 - S / maxDist) +
                        (1 - SW / maxDist) +
                        (1 - W / maxDist)
                    ) / 8)
                };
            }
        });

        HashSet<MapGridCell> reachableCells = [];

        foreach (double[] spawn in map.SpawnPositions) {
            Vector2 spawnPoint = new((float)spawn[0], (float)spawn[1]);
            MapGridCell GridLocation = Grid(geo, spawnPoint);
            IterativeFloodFill(GridLocation, grid, reachableCells);
        }

        for (int x = 0; x < width; ++x) {
            for (int y = 0; y < height; ++y) {
                if (!reachableCells.Contains(new MapGridCell(x, y))) {
                    grid[x, y] = MapGridCellData.Unwalkable;
                }
            }
        }

        return grid;
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private static void IterativeFloodFill(MapGridCell startPos, MapGridCellData[,] grid, HashSet<MapGridCell> reachableCells) {
        Queue<MapGridCell> queue = [];
        queue.Enqueue(startPos);

        while (queue.Count > 0) {
            MapGridCell pos = queue.Dequeue();

            if (pos.X < 0 || pos.X >= grid.GetLength(0) || pos.Y < 0 || pos.Y >= grid.GetLength(1)) {
                continue;
            }

            if (!grid[pos.X, pos.Y].IsWalkable || !reachableCells.Add(pos)) {
                continue;
            }

            queue.Enqueue(new(pos.X + 1, pos.Y));
            queue.Enqueue(new(pos.X - 1, pos.Y));
            queue.Enqueue(new(pos.X, pos.Y + 1));
            queue.Enqueue(new(pos.X, pos.Y - 1));
        }
    }
}
