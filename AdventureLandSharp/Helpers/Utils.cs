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

    public static MapLocation GetMapLocationForSpawn(World world, string mapName, string mobName) =>
        new(world.GetMap(mapName), world.Data.Maps[mapName].Monsters!.First(y => y.Type == mobName).GetSpawnPosition());
}
