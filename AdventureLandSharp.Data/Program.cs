using AdventureLandSharp.Core;
using AdventureLandSharp.Core.HttpApi;
using AdventureLandSharp.Core.Util;

GameData data = await Api.FetchGameDataAsync(Utils.ApiAddress);
World world = new(data);

foreach ((string mapName, GameDataMonster monster) in data.Monsters.OrderByDescending(x => x.Value.Respawn)) {
    Console.WriteLine($"{monster.Name} {monster.Respawn:f0}ms");
}
