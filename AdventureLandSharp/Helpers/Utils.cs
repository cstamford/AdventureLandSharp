using System.Diagnostics;
using System.Numerics;
using AdventureLandSharp.Core;
using AdventureLandSharp.Core.HttpApi;

namespace AdventureLandSharp.Helpers;

public static class Utils {
    public const bool InDebugMode = 
#if DEBUG
        true;
#else
        false;
#endif

    public static ApiAddress ApiAddress => new(InDebugMode ? "http://localhost:8083" : "http://adventure.land");

    public static MapLocation GetMapLocationForSpawn(World world, string mapName, string mobName) {
        (string? map, Vector2 loc) = world.Data.Maps[mapName].Monsters!
                .First(y => y.Type == mobName)
                .GetSpawnLocations()
                .First();
        Debug.Assert(map == null || map == mapName);
        return new MapLocation(world.GetMap(mapName), loc);
    }

    public static TimeSpan SafeAbilityCd(TimeSpan time) => time.Add(TimeSpan.FromMilliseconds(100));
}
