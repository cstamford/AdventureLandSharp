using System.Text.Json;
using AdventureLandSharp.Core;
using AdventureLandSharp.Core.HttpApi;
using AdventureLandSharp.Core.Util;

GameData data = await Api.FetchGameDataAsync(Utils.ApiAddress);
World world = new(data);

MapLocation[] route = Utils.CalculateOptimalVisitOrder(world, [
    world.GetMap("main").FishingSpot!.Value,
    world.GetMap("bank").DefaultSpawn,
    world.GetMap("woffice").DefaultSpawn,
    .. Utils.GetMapLocationsForSpawn(world, "main", "phoenix"),
    .. Utils.GetMapLocationsForSpawn(world, "halloween", "greenjr"),
    .. Utils.GetMapLocationsForSpawn(world, "spookytown", "jr"),
]);

Console.WriteLine(JsonSerializer.Serialize(route, JsonOpts.Default.AddMapLocationConverter(world)));

foreach (MapLocation location in route) {
    Console.WriteLine(location);
}
