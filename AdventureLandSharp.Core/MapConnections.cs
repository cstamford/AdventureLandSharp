using System.Text.Json;

namespace AdventureLandSharp.Core;

public enum MapConnectionType {
    Door,
    Transporter,
    Leave
}

public readonly record struct MapConnection(
    MapConnectionType Type,
    string SourceMap,
    float SourceX,
    float SourceY,
    string DestMap,
    float DestX,
    float DestY,
    long DestSpawnId
);

public class MapConnections(string mapName, GameData gameData, GameDataMap mapData) {
    public IReadOnlyList<MapConnection> Connections => _connections;

    private readonly List<MapConnection> _connections = CreateConnections(mapName, gameData, mapData);

    private static List<MapConnection> CreateConnections(string mapName, GameData gameData, GameDataMap mapData) {
        List<MapConnection> connections = [];

        foreach (JsonElement[] door in mapData.Doors) {
            string destMap = door[4].GetString()!;

            if (door.Any(x => x.ValueKind == JsonValueKind.String && x.GetString()!.Contains("locked"))) {
                continue; // Note: We skip locked doors.
            }

            if (gameData.Maps.TryGetValue(destMap, out GameDataMap destination)) {
                long destSpawnId = door[5].GetInt64();
                double[] destinationPosition = destination.SpawnPositions[destSpawnId];
                connections.Add(new(
                    MapConnectionType.Door,
                    mapName, (float)door[0].GetDouble(), (float)door[1].GetDouble(),
                    destMap, (float)destinationPosition[0], (float)destinationPosition[1], destSpawnId
                ));
            }
        }

        GameDataNpc transporterNpc = gameData.Npcs["transporter"];
        foreach (GameDataMapNpc npc in mapData.Npcs.Where(npc => npc.Id == "transporter")) {
            foreach ((string? destMap, long destSpawnId) in transporterNpc.Places!) {
                double[] destinationPosition = gameData.Maps[destMap].SpawnPositions[destSpawnId];
                connections.Add(new(
                    MapConnectionType.Transporter,
                    mapName, (float)npc.Position[0], (float)npc.Position[1],
                    destMap, (float)destinationPosition[0],(float)destinationPosition[1], destSpawnId
                ));
            }
        }

        double[] jailSpawn = gameData.Maps["jail"].SpawnPositions[0];
        double[] mainSpawn = gameData.Maps["main"].SpawnPositions[0];

        connections.Add(new(
            MapConnectionType.Leave,
            "jail", (float)jailSpawn[0], (float)jailSpawn[1],
            "main", (float)mainSpawn[0], (float)mainSpawn[1], 0
        ));

        return connections;
    }
}
