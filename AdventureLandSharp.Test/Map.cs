using System.Numerics;
using AdventureLandSharp.Core;

namespace AdventureLandSharp.Test;

[TestClass]
public class MapTests {
    [TestMethod]
    public void UnitConversions() {
        Map main = InitWorld.World.GetMap("main");

        List<Vector2> world = [];
        for (int y = 0; y < MapGrid.CellSize; ++y) {
            for (int x = 0; x < MapGrid.CellSize; ++x) {
                world.Add(new(main.Geometry.MinX + x, main.Geometry.MinY + y));
            }
        }

        List<MapGridCell> grid = [..world.Select(x => x.Grid(main))];
    }
}
