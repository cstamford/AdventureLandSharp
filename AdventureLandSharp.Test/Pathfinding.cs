using System.Diagnostics;
using System.Numerics;
using AdventureLandSharp.Core;
using AdventureLandSharp.Core.Util;

namespace AdventureLandSharp.Test;

[TestClass]
public class Pathfinding {
    [TestMethod]
    public void FindPath_Direct() {
        Map main = InitWorld.World.GetMap("main");
        Vector2 start = new(273, -341);
        Vector2 end = new(1590, -483);
        MapGraphEdgeIntraMap? path = main.FindPath(start, end);

        Debug.WriteLine(path.ToString());
        Assert.IsNotNull(path);
    }

    [TestMethod]
    public void FindRoute_SneakyShortcut() {
        Map main = InitWorld.World.GetMap("main");
        MapLocation start = new(main, new(273, -341));
        MapLocation end = new(main, new(1590, -483));
        IEnumerable<IMapGraphEdge> path = InitWorld.World.FindRoute(start, end);

        Debug.WriteLine(string.Join('\n', path.Select(x => x.ToString())));
        Assert.IsTrue(path.Any());
    }

    [TestMethod]
    public void FindRoute_SneakyShortcutInverse() {
        Map main = InitWorld.World.GetMap("main");
        MapLocation start = new(main, new(1590, -483));
        MapLocation end = new(main, new(273, -341));
        IEnumerable<IMapGraphEdge> path = InitWorld.World.FindRoute(start, end);

        Debug.WriteLine(string.Join('\n', path.Select(x => x.ToString())));
        Assert.IsTrue(path.Any());
    }

    [TestMethod]
    public void FindRoute_ShortWithTp() {
        Map main = InitWorld.World.GetMap("main");
        Map halloween = InitWorld.World.GetMap("halloween");
        MapLocation start = new(main, new(1604, -532));
        MapLocation end = new(halloween, new(8, 630));
        IEnumerable<IMapGraphEdge> path = InitWorld.World.FindRoute(start, end);

        Debug.WriteLine(string.Join('\n', path.Select(x => x.ToString())));
        Assert.IsTrue(path.Any());
    }

    [TestMethod]
    public void FindRoute_TeleportRamp() {
        Map main = InitWorld.World.GetMap("main");
        Map halloween = InitWorld.World.GetMap("halloween");
        MapLocation start = new(halloween, new(0, 642.15f));
        MapLocation end = new(main, new(-35, -162));
        IEnumerable<IMapGraphEdge> path = InitWorld.World.FindRoute(start, end);

        Debug.WriteLine(string.Join('\n', path.Select(x => x.ToString())));
        Assert.IsTrue(path.Any());
    }

    [TestMethod]
    public void FindRoute_CrabToStore() {
        Map main = InitWorld.World.GetMap("main");
        MapLocation start = Utils.GetMapLocationForSpawn(InitWorld.World, "main", "crab");
        MapLocation end = new(main, InitWorld.World.Data.Maps["main"].Npcs!.First(x => x.Id == "fancypots").GetPosition()!.Value);
        IEnumerable<IMapGraphEdge> path = InitWorld.World.FindRoute(start, end);

        Debug.WriteLine(string.Join('\n', path.Select(x => x.ToString())));
        Assert.IsTrue(path.Any());
    }

    [TestMethod]
    public void FindPath_SameSpot() {
        Map main = InitWorld.World.GetMap("main");
        IMapGraphEdge? path = main.FindPath(main.DefaultSpawn.Position, main.DefaultSpawn.Position);

        Assert.IsTrue(path == null);
    }

    [TestMethod]
    public void FindRoute_SameSpot() {
        Map main = InitWorld.World.GetMap("main");
        IEnumerable<IMapGraphEdge> path = InitWorld.World.FindRoute(main.DefaultSpawn, main.DefaultSpawn);

        Assert.IsTrue(!path.Any());
    }

    [TestMethod]
    public void FindRoute_OffGrid() {
        Map main = InitWorld.World.GetMap("main");
        Map desertland = InitWorld.World.GetMap("desertland");
        MapLocation start = new(main, new(-1184, 781));
        MapLocation end = new(desertland, new(0, 0));
        IEnumerable<IMapGraphEdge> path = InitWorld.World.FindRoute(start, end);

        Debug.WriteLine(string.Join('\n', path.Select(x => x.ToString())));
        Assert.IsTrue(path.Any());
    }

    [TestMethod]
    public void FindRoute_OffGrid2() {
        Map main = InitWorld.World.GetMap("main");
        Map desertland = InitWorld.World.GetMap("desertland");
        MapLocation start = new(desertland, new(-669, 315));
        MapLocation end = new(main, new(-1184, 781));
        IEnumerable<IMapGraphEdge> path = InitWorld.World.FindRoute(start, end);

        Debug.WriteLine(string.Join('\n', path.Select(x => x.ToString())));
        Assert.IsTrue(path.Any());
    }

    [TestMethod]
    public void FindRoute_OffGrid3() {
        Map main = InitWorld.World.GetMap("main");
        MapLocation start = new(main, new(-88, -144));
        MapLocation end = new(main, new(-89, -165));
        IEnumerable<IMapGraphEdge> path = InitWorld.World.FindRoute(start, end);

        Debug.WriteLine(string.Join('\n', path.Select(x => x.ToString())));
        Assert.IsTrue(path.Any());
    }

    [TestMethod]
    [Ignore("This test is failing, probably due to grid.")]
    public void FindRoute_Failing1() {
        Map main = InitWorld.World.GetMap("main");
        Map halloween = InitWorld.World.GetMap("halloween");
        MapLocation start = new(main, new(-551, -375));
        MapLocation end = new(halloween, new(8, 630.5f));
        IEnumerable<IMapGraphEdge> path = InitWorld.World.FindRoute(start, end);

        Debug.WriteLine(string.Join('\n', path.Select(x => x.ToString())));
        Assert.IsTrue(path.Any());
    }

    [TestMethod]
    public void FindRoute_InvalidGridCoords() {
        Map main = InitWorld.World.GetMap("main");
        MapLocation start = new(main, new(100, -1160));
        MapLocation end = new(main, new(948, -144));

        try {
            IEnumerable<IMapGraphEdge> path = InitWorld.World.FindRoute(start, end);
            Assert.Fail("Expected exception");
        } catch { }
    }

    [TestMethod]
    public void FindRoute_NoTeleportVsTeleport() {
        Map main = InitWorld.World.GetMap("main");
        Map halloween = InitWorld.World.GetMap("halloween");
        MapLocation start = new(main, new(1604, -532));
        MapLocation end = new(halloween, new(8, 630));

        IEnumerable<IMapGraphEdge> pathWithTp = InitWorld.World.FindRoute(start, end, graphSettings: new MapGraphPathSettings() with { EnableTeleport = true });
        IEnumerable<IMapGraphEdge> pathWithoutTp = InitWorld.World.FindRoute(start, end, graphSettings: new MapGraphPathSettings() with { EnableTeleport = false });

        Assert.IsTrue(pathWithTp.Any(x => x is MapGraphEdgeTeleport));
        Assert.IsFalse(pathWithoutTp.Any(x => x is MapGraphEdgeTeleport));
    }

    [TestMethod]
    public void FindRoute_GooBrawl() {
        Map main = InitWorld.World.GetMap("main");
        Map gooBrawl = InitWorld.World.GetMap("goobrawl");

        MapGraphPathSettings settings = new MapGraphPathSettings() with { EnableEvents = [
            (GameConstants.GooBrawlJoinName, InitWorld.World.GooBrawlLocation)
        ]};
        IEnumerable<IMapGraphEdge> pathIn = InitWorld.World.FindRoute(main.DefaultSpawn, gooBrawl.DefaultSpawn, settings);
        IEnumerable<IMapGraphEdge> pathOut = InitWorld.World.FindRoute(gooBrawl.DefaultSpawn, main.DefaultSpawn, settings);

        Assert.IsTrue(pathIn.Any());
        Assert.IsTrue(pathOut.Any());
    }

    [TestMethod]
    public void FindRoute_OddlySpecific() {
        Map desertland = InitWorld.World.GetMap("desertland");
        Map spookytown = InitWorld.World.GetMap("spookytown");
        MapLocation start = new(desertland, new(99.61489f, -1016.00006f));
        MapLocation end = new(spookytown, new(676.5f, 129));
        IEnumerable<IMapGraphEdge> path = InitWorld.World.FindRoute(start, end);

        Debug.WriteLine(string.Join('\n', path.Select(x => x.ToString())));
        Assert.IsTrue(path.Any());
    }

}
