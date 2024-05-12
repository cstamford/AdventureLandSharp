using System.Numerics;

namespace AdventureLandSharp.Core;

public static class MapExtensions {
    public static MapGridCell Grid(this Vector2 world, Map map) => map.Grid.Terrain.Grid(world);
    public static Vector2 World(this MapGridCell grid, Map map) => map.Grid.Terrain.World(grid);
    public static MapGridCellData Data(this Vector2 world, Map map) => world.Grid(map).Data(map);
    public static MapGridCellData Data(this MapGridCell grid, Map map) => map.Grid.Terrain[grid];

    public static GameDataSmapCellData RpHash(this Vector2 world, Map map) => map.Smap?.RpHash(world) ?? GameDataSmapCellData.Valid;
    public static GameDataSmapCellData RpHash(this MapGridCell grid, Map map) {
        Vector2 topLeft = map.Grid.Terrain.World(grid);
        return GetWorstSmapInCell(topLeft, world => world.RpHash(map));
    }
    public static GameDataSmapCellData PHash(this Vector2 world, Map map) => map.Smap?.PHash(world) ?? GameDataSmapCellData.Valid;
    public static GameDataSmapCellData PHash(this MapGridCell grid, Map map) {
        Vector2 topLeft = map.Grid.Terrain.World(grid);
        return GetWorstSmapInCell(topLeft, world => world.PHash(map));
    }

    private static GameDataSmapCellData GetWorstSmapInCell(Vector2 pos, Func<Vector2, GameDataSmapCellData> hash) {
        Span<GameDataSmapCellData> data = [ 
            hash(pos),
            hash(pos + new Vector2(MapGridTerrain.CellSize, 0)),
            hash(pos + new Vector2(0, MapGridTerrain.CellSize)),
            hash(pos + new Vector2(MapGridTerrain.CellSize, MapGridTerrain.CellSize))
        ];
        data.Sort((x, y) => y.Value.CompareTo(x.Value));
        return data[0];
    }
}
