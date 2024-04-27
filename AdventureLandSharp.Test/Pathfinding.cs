using System.Diagnostics;
using System.Numerics;
using AdventureLandSharp.Core;
using AdventureLandSharp.Helpers;

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
        IMapGraphEdge? path = main.FindPath(main.DefaultSpawn.Location, main.DefaultSpawn.Location);

        Assert.IsTrue(path == null);
    }

    [TestMethod]
    public void FindRoute_SameSpot() {
        Map main = InitWorld.World.GetMap("main");
        IEnumerable<IMapGraphEdge> path = InitWorld.World.FindRoute(main.DefaultSpawn, main.DefaultSpawn);

        Assert.IsTrue(!path.Any());
    }
}
