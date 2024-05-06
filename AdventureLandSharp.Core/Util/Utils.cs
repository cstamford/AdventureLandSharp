using AdventureLandSharp.Core.HttpApi;

namespace AdventureLandSharp.Core.Util;

public static class Utils {
    public const bool InDebugMode = 
#if DEBUG
        true;
#else
        false;
#endif

    public const bool TargetLocalServer = InDebugMode ? true : false;

    public static ApiAddress ApiAddress => new(TargetLocalServer ? "http://localhost:8083" : "http://adventure.land");

    public static MapLocation[] GetMapLocationsForSpawn(World world, string mapName, string mobName) => world.Data.Maps[mapName].Monsters!
        .First(x => x.Type == mobName)
        .GetSpawnLocations()
        .Select(x => new MapLocation(world.GetMap(x.MapName ?? mapName), x.Location))
        .ToArray();

    public static MapLocation GetMapLocationForSpawn(World world, string mapName, string mobName) => GetMapLocationsForSpawn(world, mapName, mobName).First();

    public static TimeSpan SafeAbilityCd(TimeSpan time) => time.Add(TimeSpan.FromMilliseconds(100));

    public static MapLocation[] CalculateOptimalVisitOrder(World world, MapLocation[] points) {
        ThreadLocal<(float Cost, MapLocation[] Points)> threadBest = new(() => new(float.MaxValue, []), trackAllValues: true);

        Parallel.ForEach(CalculateOptimalVisitPermutations(points, points.Length), permutation => {
            float cost = CalculateOptimalVisitOrderCost(world, permutation);
            (float bestCost, _) = threadBest.Value;
            if (cost < bestCost) {
                threadBest.Value = (cost, permutation);
            }
        });

        return threadBest.Values
            .OrderBy(x => x.Cost)
            .ThenBy(x => x.Points.First())
            .First().Points;
    }

    private static float CalculateOptimalVisitOrderCost(World world, MapLocation[] permutation) {
        float cost = 0;

        for (int i = 0; i < permutation.Length; ++i) {
            MapLocation start = permutation[i];
            MapLocation end = permutation[(i + 1) % permutation.Length];
            cost += world.FindRoute(start, end).Sum(x => x.Cost);
        }

        return cost;
    }

    private static IEnumerable<MapLocation[]> CalculateOptimalVisitPermutations(MapLocation[] points, int length) {
        if (length == 1) {
            yield return points;
            yield break;
        }

        foreach (MapLocation[] p in CalculateOptimalVisitPermutations(points, length - 1)) {
            for (int i = 0; i < length - 1; i++) {
                MapLocation[] copy = [..p];
                (copy[length - 1], copy[i]) = (copy[i], copy[length - 1]);
                yield return copy;
            }

            yield return p;
        }
    }
}
