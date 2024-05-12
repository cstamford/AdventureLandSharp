using System.Text.Json;
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

    public static Dictionary<string, GameDataSmap> LoadSmapData() {
        try {
            return JsonSerializer.Deserialize<JsonElement>(File.ReadAllText("smap_data.json"))
                .EnumerateObject()
                .Where(x => x.Value.ValueKind == JsonValueKind.Object)
                .ToDictionary(x => x.Name, x => new GameDataSmap(
                    x.Value.EnumerateObject().ToDictionary(y => y.Name, y => y.Value.GetUInt16())));
        } catch (Exception ex) {
            Log.Warn($"Failed to read smap_data.json due to {ex}");
        }

        return [];
    }

    public static MapLocation[] GetMapLocationsForSpawn(World world, string mapName, string mobName) => world.Data.Maps[mapName].Monsters!
        .First(x => x.Type == mobName)
        .GetSpawnLocations()
        .Select(x => new MapLocation(world.GetMap(x.MapName ?? mapName), x.Location))
        .ToArray();

    public static MapLocation GetMapLocationForSpawn(World world, string mapName, string mobName) => GetMapLocationsForSpawn(world, mapName, mobName).First();

    public static TimeSpan SafeAbilityCd(TimeSpan time) => time.Add(TimeSpan.FromMilliseconds(100));

    public static MapLocation[] CalculateOptimalVisitOrder(World world, MapLocation[] points) {
        float bestCost = float.MaxValue;
        MapLocation[] bestPermutation = [];
        MapLocation[][] permutations = [..CalculateOptimalVisitPermutations(points, points.Length)];

        Log.Info($"Calculating optimal visit order for {points.Length} points. There are {permutations.Length} permutations.");

        for (int i = 0; i < permutations.Length; ++i) {
            MapLocation[] permutation = permutations[i];
            float cost = CalculateOptimalVisitOrderCost(world, permutation);

            if (cost < bestCost) {
                bestCost = cost;
                bestPermutation = permutation;
            }
        }

        Log.Info($"Optimal visit order calculated. Cost: {bestCost}");

        return bestPermutation;
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
